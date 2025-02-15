﻿'========================================================================
'Kartris - www.kartris.com
'Copyright 2021 CACTUSOFT

'GNU GENERAL PUBLIC LICENSE v2
'This program is free software distributed under the GPL without any
'warranty.
'www.gnu.org/licenses/gpl-2.0.html

'KARTRIS COMMERCIAL LICENSE
'If a valid license.config issued by Cactusoft is present, the KCL
'overrides the GPL v2.
'www.kartris.com/t-Kartris-Commercial-License.aspx
'========================================================================
Imports CkartrisBLL
Imports CkartrisEnumerations
Imports CkartrisDisplayFunctions
Imports CkartrisDataManipulation
Imports KartSettingsManager
Imports KartrisClasses

Partial Class UserControls_Back_EditOrder
    Inherits System.Web.UI.UserControl

    Private _SelectedPaymentMethod As String = ""

    ''' <summary>
    ''' this runs when an update to data is made to trigger the animation
    ''' </summary>
    ''' <remarks></remarks>
    Public Event ShowMasterUpdate()

    ''' <summary>
    ''' this runs each time the page is called
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        'New instance
        Dim objOrdersBLL As New OrdersBLL
        Dim objUsersBLL As New UsersBLL

        Dim numCustomerID As Integer = 0


        If Not Page.IsPostBack Then
            ViewState("Referer") = Request.ServerVariables("HTTP_REFERER")
            Try
                'Payment gateways
                Dim strPaymentMethods As String = GetKartConfig("frontend.payment.gatewayslist")
                Dim arrPaymentsMethods As String() = Split(strPaymentMethods, ",")
                Try
                    ddlPaymentGateways.Items.Add(New ListItem(GetGlobalResourceObject("Kartris", "ContentText_DropdownSelectDefault"), ""))
                    For Each strGatewayEntry As String In arrPaymentsMethods
                        Dim arrGateway As String() = Split(strGatewayEntry, "::")
                        If UBound(arrGateway) = 4 Then
                            Dim blnOkToAdd As Boolean = True
                            If arrGateway(4) = "p" Then
                                If LCase(arrGateway(1)) = "test" Then
                                    blnOkToAdd = HttpSecureCookie.IsBackendAuthenticated
                                ElseIf LCase(arrGateway(1)) = "off" Then
                                    blnOkToAdd = False
                                End If
                            Else
                                blnOkToAdd = False
                            End If
                            If blnOkToAdd Then
                                Dim strGatewayName As String = arrGateway(0)
                                If strGatewayName.ToLower = "po_offlinepayment" Then
                                    strGatewayName = GetGlobalResourceObject("Checkout", "ContentText_Po")
                                End If
                                ddlPaymentGateways.Items.Add(New ListItem(strGatewayName, arrGateway(0).ToString))
                            End If
                        Else
                            Throw New Exception("Invalid gatewaylist config setting!")
                        End If
                    Next
                Catch ex As Exception
                    Throw New Exception("Error loading payment gateway list")
                End Try

                'Check we have a payment system
                If ddlPaymentGateways.Items.Count = 1 Then
                    Throw New Exception("No valid payment gateways")
                End If

                'Get the Order ID QueryString - if it won't convert to integer then force return to Orders List page
                ViewState("numOrderID") = CType(Request.QueryString("OrderID"), Integer)
                If ViewState("numOrderID") = 0 Then
                    'jeepers, let's hope this doesn't happen
                Else
                    Dim dtOrderRecord As DataTable = objOrdersBLL.GetOrderByID(ViewState("numOrderID"))

                    If dtOrderRecord IsNot Nothing Then
                        If dtOrderRecord.Rows.Count = 1 Then
                            Dim strOrderData As String = ""
                            litOrderID.Text = dtOrderRecord.Rows(0)("O_ID")

                            'Process the order text
                            Dim strOrderText As String = CkartrisDataManipulation.FixNullFromDB(dtOrderRecord.Rows(0)("O_Details"))
                            If InStr(Server.HtmlDecode(strOrderText).ToLower, "</html>") > 0 Then
                                strOrderText = CkartrisBLL.ExtractHTMLBodyContents(Server.HtmlDecode(strOrderText))
                                strOrderText = strOrderText.Replace("[orderid]", dtOrderRecord.Rows(0)("O_ID"))
                            Else
                                strOrderText = Replace(strOrderText, vbCrLf, "<br/>").Replace(vbLf, "<br/>")
                            End If
                            litOrderText.Text = strOrderText

                            strOrderData = CkartrisDataManipulation.FixNullFromDB(dtOrderRecord.Rows(0)("O_Data"))
                            Dim intOrderLanguageID As Int16 = CShort(dtOrderRecord.Rows(0)("O_LanguageID"))
                            Dim intOrderCurrencyID As Int16 = CShort(dtOrderRecord.Rows(0)("O_CurrencyID"))
                            Dim strOrderPaymentGateway As String = dtOrderRecord.Rows(0)("O_PaymentGateway")
                            lnkOrderCustomerID.Text = CInt(dtOrderRecord.Rows(0)("O_CustomerID"))

                            numCustomerID = CInt(dtOrderRecord.Rows(0)("O_CustomerID"))

                            lnkOrderCustomerID.NavigateUrl = FormatCustomerLink(dtOrderRecord.Rows(0)("O_CustomerID"))
                            txtOrderCustomerEmail.Text = UsersBLL.CleanGuestEmailUsername(objUsersBLL.GetEmailByID(lnkOrderCustomerID.Text))
                            txtOrderPONumber.Text = CkartrisDataManipulation.FixNullFromDB(dtOrderRecord.Rows(0)("O_PurchaseOrderNo"))
                            chkOrderSent.Checked = CBool(dtOrderRecord.Rows(0)("O_Sent"))
                            chkOrderInvoiced.Checked = CBool(dtOrderRecord.Rows(0)("O_Invoiced"))
                            chkSendOrderUpdateEmail.Checked = CBool(dtOrderRecord.Rows(0)("O_SendOrderUpdateEmail"))

                            Try
                                litComments.Text = dtOrderRecord.Rows(0)("O_Comments")
                            Catch ex As Exception
                                'Probably edited order
                            End Try

                            Session("LANG") = intOrderLanguageID
                            Session("CUR_ID") = intOrderCurrencyID

                            'Build up the language dropdown
                            ddlOrderLanguage.Items.Clear()
                            For Each objLang As Language In Language.LoadLanguages
                                Dim lstItem As New ListItem(objLang.Name, objLang.ID)
                                ddlOrderLanguage.Items.Add(lstItem)
                            Next

                            ddlOrderLanguage.SelectedValue = intOrderLanguageID

                            ViewState("arrOrderData") = Split(strOrderData, "|||")
                            Dim objOrder As New Kartris.Interfaces.objOrder
                            objOrder = Payment.Deserialize(ViewState("arrOrderData")(0), objOrder.GetType)
                            Dim addBilling As Address = Nothing
                            Dim addShipping As Address = Nothing

                            With objOrder
                                With .Billing
                                    addBilling = New Address(.Name, .Company, .StreetAddress, .TownCity, .CountyState, .Postcode,
                                                                          Country.GetByIsoCode(.CountryIsoCode).CountryId, .Phone, , ,
                                                                          IIf(objOrder.SameShippingAsBilling, "u", "b"))
                                End With
                                If .SameShippingAsBilling Then
                                    addShipping = addBilling
                                    ViewState("intShippingDestinationID") = Country.GetByIsoCode(.Billing.CountryIsoCode).CountryId
                                Else
                                    With .Shipping
                                        addShipping = New Address(.Name, .Company, .StreetAddress, .TownCity, .CountyState, .Postcode,
                                                                          Country.GetByIsoCode(.CountryIsoCode).CountryId, .Phone, , ,
                                                                          "s")
                                        ViewState("intShippingDestinationID") = Country.GetByIsoCode(.CountryIsoCode).CountryId
                                    End With
                                End If
                            End With

                            UC_BillingAddress.CustomerID = numCustomerID
                            UC_ShippingAddress.CustomerID = numCustomerID

                            'Set up first user address (billing)
                            Dim lstUsrAddresses As Collections.Generic.List(Of KartrisClasses.Address) = Nothing

                            '---------------------------------------
                            'BILLING ADDRESS
                            '---------------------------------------
                            If UC_BillingAddress.Addresses Is Nothing Then

                                'Find all addresses in this user's account
                                lstUsrAddresses = KartrisClasses.Address.GetAll(numCustomerID)

                                'Populate dropdown by filtering billing/universal addresses
                                UC_BillingAddress.Addresses = lstUsrAddresses.FindAll(Function(p) p.Type = "b" Or p.Type = "u")

                                'Try to select correct address based on postcode of order,
                                'this avoids having to manually select correct addresses when
                                'cloning and editing an order
                                UC_BillingAddress.SelectByPostcode(objOrder.Billing.Postcode)
                            End If

                            '---------------------------------------
                            'SHIPPING ADDRESS
                            '---------------------------------------
                            If UC_ShippingAddress.Addresses Is Nothing Then

                                'Find all addresses in this user's account
                                If lstUsrAddresses Is Nothing Then lstUsrAddresses = KartrisClasses.Address.GetAll(numCustomerID)

                                'Populate dropdown by filtering shipping/universal addresses
                                UC_ShippingAddress.Addresses = lstUsrAddresses.FindAll(Function(ShippingAdd) ShippingAdd.Type = "s" Or ShippingAdd.Type = "u")

                                'Try to select correct address based on postcode of order,
                                'this avoids having to manually select correct addresses when
                                'cloning and editing an order
                                UC_ShippingAddress.SelectByPostcode(objOrder.Shipping.Postcode)
                            End If

                            If objOrder.Shipping.CountryIsoCode Is Nothing Then
                                objOrder.SameShippingAsBilling = True
                                chkSameShippingAsBilling.Checked = True
                                chkSameShippingAsBilling_CheckedChanged(Nothing, Nothing)
                            End If

                            If ddlPaymentGateways.Items.Count = 2 And strOrderPaymentGateway = "" Then
                                _SelectedPaymentMethod = ddlPaymentGateways.Items(1).Value
                                ddlPaymentGateways.SelectedValue = ddlPaymentGateways.Items(1).Value
                                valPaymentGateways.Enabled = False 'disable validation just to be sure
                            End If

                            If _SelectedPaymentMethod = "" Then
                                _SelectedPaymentMethod = strOrderPaymentGateway
                                ddlPaymentGateways.SelectedValue = strOrderPaymentGateway
                            End If
                            LoadBasket()
                        Else
                            Exit Sub
                        End If
                    Else
                        Exit Sub
                    End If
                End If
            Catch ex As Exception
                CkartrisFormatErrors.LogError(ex.Message)
                Response.Redirect("_OrdersList.aspx")
            End Try
        End If

        numCustomerID = lnkOrderCustomerID.Text

        'Set basket to user ID, so we get customer discount if required
        'Try to set User ID
        Try
            _UC_BasketMain.UserID = numCustomerID
        Catch ex As Exception

        End Try
    End Sub

    ''' <summary>
    ''' Load the basket; if we pass TRUE into this, it
    ''' will use a second basket object to load up the 
    ''' old order data from the viewstate and then run
    ''' each item into the main basket and save it
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub LoadBasket(Optional ByVal blnCopyOrderItems As Boolean = False)
        Dim objBasket As Kartris.Basket = UC_BasketMain.GetBasket
        Dim sessionID As Long = Session("SessionID")

        If blnCopyOrderItems Then
            Dim objBasketTemp As Kartris.Basket = UC_BasketMain.GetBasket
            objBasketTemp = Payment.Deserialize(ViewState("arrOrderData")(1), objBasketTemp.GetType)
            UC_BasketMain.EmptyBasket_Click(Nothing, Nothing)

            If objBasketTemp.BasketItems IsNot Nothing Then
                Dim BasketItem As New BasketItem
                'final check if basket items are still there
                For Each item As BasketItem In objBasketTemp.BasketItems
                    With item
                        BasketBLL.AddNewBasketValue(objBasket.BasketItems, BasketBLL.BASKET_PARENTS.BASKET, sessionID, .VersionID, .Quantity, .CustomText, .OptionText)
                    End With
                Next
            End If
        End If

        'get the shipping destination id from the viewstate and assign it to the basket controls destination id property
        UC_BasketMain.ShippingDestinationID = ViewState("intShippingDestinationID")

        'reload the basketitems from the database - this confirms if the items were correctly added from the invoicerows data
        objBasket.LoadBasketItems()

        'save it
        Session("Basket") = objBasket
        Session("BasketItems") = objBasket.BasketItems

        'recalculate and refresh display
        UC_BasketMain.LoadBasket()
        UC_BasketMain.RefreshShippingMethods()
    End Sub

    ''' <summary>
    ''' Format back link
    ''' </summary>
    ''' <remarks></remarks>
    Public Function FormatBackLink(ByVal strDate As String, ByVal strFromDate As String, ByVal strPage As String) As String
        Dim strURL As String = ""
        If strFromDate = "false" And (strPage = "" Or strPage = "1") Then
            'No page or date passed, format js back link
            strURL = "javascript:history.back()"
        Else
            'We have either a date, or page number, or both
            If strFromDate = "true" Then
                strURL &= "_OrdersList.aspx?strDate=" & strDate
            Else
                strURL &= "_OrdersList.aspx?strDate="
            End If
            If CInt(strPage) > 1 Then
                strURL &= "&Page=" & strPage
            End If
        End If
        Return strURL
    End Function

    ''' <summary>
    ''' Handle checkbox for same shipping/billing
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub chkSameShippingAsBilling_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles chkSameShippingAsBilling.CheckedChanged
        If chkSameShippingAsBilling.Checked Then
            pnlShippingAddress.Visible = False
            RefreshShippingMethods("billing")
        Else
            pnlShippingAddress.Visible = True
            RefreshShippingMethods("shipping")
        End If
    End Sub

    ''' <summary>
    ''' Refresh shipping methods
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub RefreshShippingMethods(Optional ByVal strControl As String = "shipping")
        Dim numShippingDestinationID As Integer
        If strControl = "billing" Then
            If UC_BillingAddress.SelectedAddress IsNot Nothing Then
                numShippingDestinationID = UC_BillingAddress.SelectedAddress.CountryID
            Else
                numShippingDestinationID = 0
            End If
        Else
            If UC_ShippingAddress.SelectedAddress IsNot Nothing Then
                numShippingDestinationID = UC_ShippingAddress.SelectedAddress.CountryID
            Else
                numShippingDestinationID = 0
            End If
        End If

        'Check whether to show the EUVat Field or not
        If Not String.IsNullOrEmpty(GetKartConfig("general.tax.euvatcountry")) Then
            Dim adrShipping As Address = Nothing
            If chkSameShippingAsBilling.Checked Then
                If UC_BillingAddress.SelectedID > 0 Then
                    adrShipping = UC_BillingAddress.Addresses.Find(Function(Add) Add.ID = UC_BillingAddress.SelectedID)
                ElseIf numShippingDestinationID > 0 Then
                    adrShipping = UC_BillingAddress.SelectedAddress
                End If
            Else
                If UC_ShippingAddress.SelectedID > 0 Then
                    adrShipping = UC_ShippingAddress.Addresses.Find(Function(Add) Add.ID = UC_ShippingAddress.SelectedID)
                ElseIf numShippingDestinationID > 0 Then
                    adrShipping = UC_ShippingAddress.SelectedAddress
                End If
            End If
            If adrShipping IsNot Nothing Then
                If UCase(adrShipping.Country.IsoCode) <> UCase(GetKartConfig("general.tax.euvatcountry")) And adrShipping.Country.D_Tax Then
                    phdEUVAT.Visible = True
                    litMSCode.Text = adrShipping.Country.IsoCode
                    If litMSCode.Text = "GR" Then litMSCode.Text = "EL"
                Else
                    phdEUVAT.Visible = False
                End If
            Else
                phdEUVAT.Visible = False
            End If
        Else
            phdEUVAT.Visible = False
        End If

        txtEUVAT.Text = ""
        Session("blnEUVATValidated") = False

        Dim objShippingDetails As New Interfaces.objShippingDetails

        UC_BasketMain.ShippingDetails = objShippingDetails
        UC_BasketMain.ShippingDestinationID = numShippingDestinationID
        UC_BasketMain.RefreshShippingMethods()

    End Sub

    ''' <summary>
    ''' Cancel button
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub lnkBtnCancel_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Response.Redirect(ViewState("Referer"))
    End Sub

    '''' <summary>
    '''' Reset basket from order data button was clicked
    '''' </summary>
    '''' <remarks></remarks>
    Protected Sub lnkBtnResetAndCopy_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        BasketBLL.DeleteBasket() 'clear basket when restoring
        UC_BasketMain.EmptyBasket_Click(Nothing, Nothing)
        LoadBasket(True)
        RaiseEvent ShowMasterUpdate()
    End Sub

    ''' <summary>
    ''' Add to basket on lookup box clicked
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub lnkBtnAddToBasket_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim intVersionID As Long = CheckAutoCompleteData()
        If intVersionID > 0 Then
            Dim objVersionsBLL As New VersionsBLL
            Dim dr As DataRow = objVersionsBLL._GetVersionByID(intVersionID).Rows(0)
            Select Case CChar(FixNullFromDB(dr("V_Type")))
                Case "o", "b", "c"  '' Options Product
                    litOptionsVersion.Text = intVersionID
                    _UC_AutoComplete_Item.SetText("")
                    _UC_OptionsPopup.ShowPopup(intVersionID, FixNullFromDB(dr("V_ProductID")), FixNullFromDB(dr("V_CodeNumber"))) '' Show options for selected product
                Case Else           '' Normal Product
                    litOptionsVersion.Text = ""
                    Dim objBasket As Kartris.Basket = UC_BasketMain.GetBasket
                    Dim sessionID As Long = Session("SessionID")
                    BasketBLL.AddNewBasketValue(objBasket.BasketItems, BasketBLL.BASKET_PARENTS.BASKET, sessionID, intVersionID, 1, "", "")
                    _UC_AutoComplete_Item.SetText("")
                    LoadBasket()
            End Select
            RaiseEvent ShowMasterUpdate()
        End If
    End Sub

    ''' <summary>
    ''' Save button
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub lnkBtnSave_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.Confirmation, GetLocalResourceObject("ContentText_ConfirmEditOrder"))
    End Sub

    ''' <summary>
    ''' Confirmed save
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub _UC_PopupMsg_Confirmed() Handles _UC_PopupMsg.Confirmed
        Dim CUR_ID As Integer = CInt(Session("CUR_ID"))

        Dim blnUseHTMLOrderEmail As Boolean = (GetKartConfig("general.email.enableHTML") = "y")
        Dim sbdHTMLOrderEmail As StringBuilder = New StringBuilder
        Dim sbdHTMLOrderContents As StringBuilder = New StringBuilder
        Dim sbdHTMLOrderBasket As StringBuilder = New StringBuilder

        Dim sbdNewCustomerEmailText As StringBuilder = New StringBuilder
        Dim sbdBodyText As StringBuilder = New StringBuilder
        Dim sbdBasketItems As StringBuilder = New StringBuilder

        Dim strSubject As String = ""
        Dim strTempEmailTextHolder As String = ""

        Dim arrBasketItems As List(Of Kartris.BasketItem)
        Dim objBasket As Kartris.Basket = Session("Basket")
        Dim objOrder As Kartris.Interfaces.objOrder = Nothing
        Dim objObjectConfigBLL As New ObjectConfigBLL
        Dim strOrderDetails As String = ""

        Dim blnAppPricesIncTax As Boolean
        Dim blnAppShowTaxDisplay As Boolean
        Dim blnAppUSmultistatetax As Boolean
        If ConfigurationManager.AppSettings("TaxRegime").ToLower = "us" Or ConfigurationManager.AppSettings("TaxRegime").ToLower = "simple" Then
            blnAppPricesIncTax = False
            blnAppShowTaxDisplay = False
            blnAppUSmultistatetax = True
        Else
            blnAppPricesIncTax = GetKartConfig("general.tax.pricesinctax") = "y"
            blnAppShowTaxDisplay = GetKartConfig("frontend.display.showtax") = "y"
            blnAppUSmultistatetax = False
        End If

        Dim intGatewayCurrency As Int16 = 0

        'Get the order confirmation template if HTML email is enabled
        If blnUseHTMLOrderEmail Then
            sbdHTMLOrderEmail.Append(RetrieveHTMLEmailTemplate("OrderConfirmationEdit"))

            'This was a new template, so if not in skin, let's pull the older one
            If sbdHTMLOrderEmail.Length < 1 Then
                sbdHTMLOrderEmail.Append(RetrieveHTMLEmailTemplate("OrderConfirmation"))
            End If

            'switch back to normal text email if the template can't be retrieved
            If sbdHTMLOrderEmail.Length < 1 Then
                blnUseHTMLOrderEmail = False
            End If
        End If

        Dim objOrdersBLL As New OrdersBLL

        'try to get the gateway currency and details from the old order
        Dim intO_ID As Integer = ViewState("numOrderID")
        Dim dtOrderRecord As DataTable = objOrdersBLL.GetOrderByID(intO_ID)
        If dtOrderRecord IsNot Nothing Then
            If dtOrderRecord.Rows.Count = 1 Then
                intGatewayCurrency = dtOrderRecord.Rows(0)("O_CurrencyIDGateway")
                strOrderDetails = dtOrderRecord.Rows(0)("O_Details")
            Else
                Exit Sub
            End If
        Else
            Exit Sub
        End If

        '============================================
        'FORMAT ORDER EMAIL
        '============================================

        'Handle Promotion Coupons
        If Not String.IsNullOrEmpty(objBasket.CouponName) And objBasket.CouponDiscount.IncTax = 0 Then
            strTempEmailTextHolder = GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker") & vbCrLf & " " & GetGlobalResourceObject("Basket", "ContentText_ApplyCouponCode") & vbCrLf & " " & objBasket.CouponName & vbCrLf
            sbdBodyText.AppendLine(strTempEmailTextHolder)
            If blnUseHTMLOrderEmail Then
                sbdHTMLOrderContents.Append(GetBasketModifierHTMLEmailText(objBasket.CouponDiscount, GetGlobalResourceObject("Kartris", "ContentText_CouponDiscount"), objBasket.CouponName))
            End If
        End If

        'Promotion discount
        Dim strPromotionDescription As String = ""
        If objBasket.PromotionDiscount.IncTax < 0 Then
            For Each objPromotion As PromotionBasketModifier In UC_BasketMain.GetPromotionsDiscount
                With objPromotion
                    sbdBodyText.AppendLine(GetItemEmailText(.Quantity & " x " & GetGlobalResourceObject("Kartris", "ContentText_PromotionDiscount"), .Name, .ExTax, .IncTax, .TaxAmount, .ComputedTaxRate))
                    If blnUseHTMLOrderEmail Then
                        sbdHTMLOrderContents.Append(GetHTMLEmailRowText(.Quantity & " x " & GetGlobalResourceObject("Kartris", "ContentText_PromotionDiscount"), .Name, .ExTax, .IncTax, .TaxAmount, .ComputedTaxRate))
                    End If
                    If strPromotionDescription <> "" Then strPromotionDescription += vbCrLf & .Name Else strPromotionDescription += .Name
                End With
            Next
        End If

        'Coupon discount
        If objBasket.CouponDiscount.IncTax < 0 Then
            sbdBodyText.AppendLine(GetBasketModifierEmailText(objBasket.CouponDiscount, GetGlobalResourceObject("Kartris", "ContentText_CouponDiscount"), objBasket.CouponName))
            If blnUseHTMLOrderEmail Then
                sbdHTMLOrderContents.Append(GetBasketModifierHTMLEmailText(objBasket.CouponDiscount, GetGlobalResourceObject("Kartris", "ContentText_CouponDiscount"), objBasket.CouponName))
            End If
        End If

        'Customer discount
        'We need to show this line if the discount exists (i.e. not zero) but
        'also if zero but there are items that are exempt from the discount, so
        'it's clear why the discount is zero.
        If objBasket.CustomerDiscount.IncTax < 0 Or objBasket.HasCustomerDiscountExemption Then
            sbdBodyText.AppendLine(GetBasketModifierEmailText(objBasket.CustomerDiscount, GetGlobalResourceObject("Basket", "ContentText_Discount"), "[customerdiscountexempttext]"))
            If blnUseHTMLOrderEmail Then
                sbdHTMLOrderContents.Append(GetBasketModifierHTMLEmailText(objBasket.CustomerDiscount, GetGlobalResourceObject("Basket", "ContentText_Discount"), "[customerdiscountexempttext]"))
            End If
        End If

        'Shipping
        sbdBodyText.AppendLine(GetBasketModifierEmailText(objBasket.ShippingPrice, GetGlobalResourceObject("Address", "ContentText_Shipping"), IIf(String.IsNullOrEmpty(objBasket.ShippingDescription), objBasket.ShippingName, objBasket.ShippingDescription)))
        If blnUseHTMLOrderEmail Then
            sbdHTMLOrderContents.Append(GetBasketModifierHTMLEmailText(objBasket.ShippingPrice, GetGlobalResourceObject("Address", "ContentText_Shipping"), IIf(String.IsNullOrEmpty(objBasket.ShippingDescription), objBasket.ShippingName, objBasket.ShippingDescription)))
        End If

        'Order handling charge
        If objBasket.OrderHandlingPrice.ExTax > 0 Then
            sbdBodyText.AppendLine(GetBasketModifierEmailText(objBasket.OrderHandlingPrice, GetGlobalResourceObject("Kartris", "ContentText_OrderHandlingCharge"), ""))
            If blnUseHTMLOrderEmail Then
                sbdHTMLOrderContents.Append(GetBasketModifierHTMLEmailText(objBasket.OrderHandlingPrice, GetGlobalResourceObject("Kartris", "ContentText_OrderHandlingCharge"), ""))
            End If
        End If

        sbdBodyText.AppendLine(GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker"))

        'Order totals
        If blnAppPricesIncTax = False Or blnAppShowTaxDisplay Then
            sbdBodyText.AppendLine(" " & GetGlobalResourceObject("Checkout", "ContentText_OrderValue") & " = " & CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.FinalPriceExTax, , False) & vbCrLf)
            sbdBodyText.Append(" " & GetGlobalResourceObject("Kartris", "ContentText_Tax") & " = " & CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.FinalPriceTaxAmount, , False) &
                         IIf(blnAppUSmultistatetax, " (" & Math.Round((objBasket.D_Tax * 100), 5) & "%)", "") & vbCrLf)
        End If
        Dim objLanguageElementsBLL As New LanguageElementsBLL()
        sbdBodyText.Append(" " & GetGlobalResourceObject("Basket", "ContentText_TotalInclusive") & " = " & CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.FinalPriceIncTax, , False) &
                                   " (" & CurrenciesBLL.CurrencyCode(CUR_ID) & " - " &
                                        objLanguageElementsBLL.GetElementValue(GetLanguageIDfromSession,
                                        CkartrisEnumerations.LANG_ELEM_TABLE_TYPE.Currencies,
                                        CkartrisEnumerations.LANG_ELEM_FIELD_NAME.Name, CUR_ID) &
                                    ")" & vbCrLf)
        sbdBodyText.AppendLine(GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker"))
        If blnUseHTMLOrderEmail Then
            sbdHTMLOrderContents.Append("<tr class=""row_totals""><td colspan=""2"">")
            If blnAppPricesIncTax = False Or blnAppShowTaxDisplay Then
                sbdHTMLOrderContents.AppendLine(" " & GetGlobalResourceObject("Checkout", "ContentText_OrderValue") & " = " & CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.FinalPriceExTax, , False) & "<br/>")
                sbdHTMLOrderContents.Append(" " & GetGlobalResourceObject("Kartris", "ContentText_Tax") & " = " & CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.FinalPriceTaxAmount, , False) &
                             IIf(blnAppUSmultistatetax, " (" & Math.Round((objBasket.D_Tax * 100), 5) & "%)", "") & "<br/>")
            End If
            sbdHTMLOrderContents.Append("(" & CurrenciesBLL.CurrencyCode(CUR_ID) & " - " &
                                                    objLanguageElementsBLL.GetElementValue(GetLanguageIDfromSession,
                                                    CkartrisEnumerations.LANG_ELEM_TABLE_TYPE.Currencies,
                                                    CkartrisEnumerations.LANG_ELEM_FIELD_NAME.Name, CUR_ID) &
                                                ") <strong>" & GetGlobalResourceObject("Basket", "ContentText_TotalInclusive") & " = " & CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.FinalPriceIncTax, , False) &
                                                "</strong></td></tr>")
        End If

        'Handle order total conversion to different currency.
        'Some payment systems only accept one currency, e.g.
        'USD. In this case, if you have multiple currencies
        'on your site, the amount needs to be converted to
        'this one currency in order to process the payment on
        'the payment gateway.
        Dim numGatewayTotalPrice As Double


        Dim blnDifferentGatewayCurrency As Boolean = CUR_ID <> intGatewayCurrency
        Dim blnDifferentOrderCurrency As Boolean = CurrenciesBLL.GetDefaultCurrency <> CUR_ID

        If blnDifferentGatewayCurrency Then
            numGatewayTotalPrice = CurrenciesBLL.FormatCurrencyPrice(intGatewayCurrency, CurrenciesBLL.ConvertCurrency(intGatewayCurrency, objBasket.FinalPriceIncTax, CUR_ID), False, False)
            'If clsPlugin.GatewayName.ToLower = "bitcoin" Then numGatewayTotalPrice = Math.Round(numGatewayTotalPrice, 8)

            sbdBodyText.AppendLine(" " & GetGlobalResourceObject("Email", "EmailText_ProcessCurrencyExp1") & vbCrLf)
            sbdBodyText.Append(" " & GetGlobalResourceObject("Email", "ContentText_TotalInclusive") & " = " & CurrenciesBLL.FormatCurrencyPrice(intGatewayCurrency, numGatewayTotalPrice, , False) &
                                       " (" & CurrenciesBLL.CurrencyCode(intGatewayCurrency) & " - " &
                                           objLanguageElementsBLL.GetElementValue(GetLanguageIDfromSession,
                                           CkartrisEnumerations.LANG_ELEM_TABLE_TYPE.Currencies,
                                           CkartrisEnumerations.LANG_ELEM_FIELD_NAME.Name, intGatewayCurrency) &
                                         ")" & vbCrLf)
            sbdBodyText.Append(GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker") & vbCrLf)

            If blnUseHTMLOrderEmail Then
                sbdHTMLOrderContents.Append("<tr class=""row_processcurrency""><td colspan=""2"">")
                sbdHTMLOrderContents.AppendLine(" " & GetGlobalResourceObject("Email", "EmailText_ProcessCurrencyExp1") & "<br/>")
                sbdHTMLOrderContents.Append(" " & GetGlobalResourceObject("Email", "ContentText_TotalInclusive") & " = " & CurrenciesBLL.FormatCurrencyPrice(intGatewayCurrency, numGatewayTotalPrice, , False) &
                                                    " (" & CurrenciesBLL.CurrencyCode(intGatewayCurrency) & " - " &
                                                       objLanguageElementsBLL.GetElementValue(GetLanguageIDfromSession,
                                                       CkartrisEnumerations.LANG_ELEM_TABLE_TYPE.Currencies,
                                                       CkartrisEnumerations.LANG_ELEM_FIELD_NAME.Name, intGatewayCurrency) &
                                                     ")" & "<br/>")
                sbdHTMLOrderContents.Append("</td></tr>")
            End If
        Else
            'User was using same currency as the gateway requires, or
            'the gateway supports multiple currencies... no conversion
            'needed.
            numGatewayTotalPrice = objBasket.FinalPriceIncTax
        End If

        'Total Saved
        If objBasket.TotalAmountSaved > 0 And KartSettingsManager.GetKartConfig("frontend.checkout.confirmation.showtotalsaved") = "y" Then
            strTempEmailTextHolder = " " & GetGlobalResourceObject("Email", "EmailText_TotalSaved1") & CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.TotalAmountSaved, , False) & GetGlobalResourceObject("Email", "EmailText_TotalSaved2") & vbCrLf
            sbdBodyText.AppendLine(strTempEmailTextHolder)
            sbdBodyText.Append(GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker"))
            If blnUseHTMLOrderEmail Then
                sbdHTMLOrderEmail.Replace("[totalsavedline]", strTempEmailTextHolder.Replace(vbCrLf, "<br/>"))
            End If
        Else
            sbdHTMLOrderEmail.Replace("[totalsavedline]", "")
        End If

        'Customer billing information
        sbdBodyText.Append(vbCrLf)
        With UC_BillingAddress.SelectedAddress
            sbdBodyText.Append(" " & GetGlobalResourceObject("Email", "EmailText_PurchaseContactDetails") & vbCrLf)

            sbdBodyText.Append(" " & GetGlobalResourceObject("Address", "FormLabel_CardHolderName") & ": " & .FullName & vbCrLf &
                                             " " & GetGlobalResourceObject("Email", "EmailText_Company") & ": " & .Company & vbCrLf &
                                             IIf(Not String.IsNullOrEmpty(txtEUVAT.Text), " " & GetGlobalResourceObject("Invoice", "FormLabel_CardholderEUVatNum") & ": " & txtEUVAT.Text & vbCrLf, ""))


            sbdBodyText.Append(" " & GetGlobalResourceObject("Kartris", "FormLabel_EmailAddress") & ": " & txtOrderCustomerEmail.Text & vbCrLf)

            sbdBodyText.Append(" " & GetGlobalResourceObject("Address", "FormLabel_Telephone") & ": " & .Phone & vbCrLf & vbCrLf)

            sbdBodyText.Append(" " & GetGlobalResourceObject("Email", "EmailText_Address") & ":" & vbCrLf)

            sbdBodyText.Append(" " & .StreetAddress & vbCrLf &
                            " " & .TownCity & vbCrLf &
                            " " & .County & vbCrLf &
                            " " & .Postcode & vbCrLf &
                            " " & .Country.Name)

            sbdBodyText.Append(vbCrLf & vbCrLf & GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker") & vbCrLf)

            If blnUseHTMLOrderEmail Then

                sbdHTMLOrderEmail.Replace("[billingname]", Server.HtmlEncode(.FullName))
                'retrieve the company label text and value if present, otherwise hide both placeholders from the template
                If Not String.IsNullOrEmpty(.Company) Then
                    sbdHTMLOrderEmail.Replace("[companylabel]", GetGlobalResourceObject("Email", "EmailText_Company") & ": ")
                    sbdHTMLOrderEmail.Replace("[billingcompany]", Server.HtmlEncode(.Company))
                Else
                    sbdHTMLOrderEmail.Replace("[companylabel]", "")
                    sbdHTMLOrderEmail.Replace("[billingcompany]<br />", "")
                    sbdHTMLOrderEmail.Replace("[billingcompany]", "")
                End If
                'do the same for EUVAT number
                If Not String.IsNullOrEmpty(txtEUVAT.Text) Then
                    sbdHTMLOrderEmail.Replace("[euvatnumberlabel]", GetGlobalResourceObject("Invoice", "FormLabel_CardholderEUVatNum") & ": ")
                    sbdHTMLOrderEmail.Replace("[euvatnumbervalue]", Server.HtmlEncode(txtEUVAT.Text))
                Else
                    sbdHTMLOrderEmail.Replace("[euvatnumberlabel]", "")
                    sbdHTMLOrderEmail.Replace("[euvatnumbervalue]<br />", "")
                    sbdHTMLOrderEmail.Replace("[euvatnumbervalue]", "")
                End If
                'do the same for EORI number
                If Not String.IsNullOrEmpty(objObjectConfigBLL.GetValue("K:user.eori", UC_BillingAddress.CustomerID)) Then
                    sbdHTMLOrderEmail.Replace("[eorinumberlabel]", "EORI: ")
                    sbdHTMLOrderEmail.Replace("[eorinumbervalue]", objObjectConfigBLL.GetValue("K:user.eori", UC_BillingAddress.CustomerID))
                Else
                    sbdHTMLOrderEmail.Replace("[eorinumberlabel]", "")
                    sbdHTMLOrderEmail.Replace("[eorinumbervalue]<br />", "")
                    sbdHTMLOrderEmail.Replace("[eorinumbervalue]", "")
                End If
                sbdHTMLOrderEmail.Replace("[billingemail]", Server.HtmlEncode(txtOrderCustomerEmail.Text))
                sbdHTMLOrderEmail.Replace("[billingphone]", Server.HtmlEncode(.Phone))
                sbdHTMLOrderEmail.Replace("[billingstreetaddress]", Server.HtmlEncode(.StreetAddress))
                sbdHTMLOrderEmail.Replace("[billingtowncity]", Server.HtmlEncode(.TownCity))
                sbdHTMLOrderEmail.Replace("[billingcounty]", Server.HtmlEncode(.County))
                sbdHTMLOrderEmail.Replace("[billingpostcode]", Server.HtmlEncode(.Postcode))
                sbdHTMLOrderEmail.Replace("[billingcountry]", Server.HtmlEncode(.Country.Name))

            End If
        End With

        'Shipping info
        sbdBodyText.Append(" " & GetGlobalResourceObject("Email", "EmailText_ShippingDetails") & vbCrLf)
        Dim strShippingAddressEmailText As String = ""


        If chkSameShippingAsBilling.Checked Then
            With UC_BillingAddress.SelectedAddress
                strShippingAddressEmailText = " " & .FullName & vbCrLf & " " & .Company & vbCrLf &
                                          " " & .StreetAddress & vbCrLf & " " & .TownCity & vbCrLf &
                                          " " & .County & vbCrLf & " " & .Postcode & vbCrLf &
                                          " " & .Country.Name & vbCrLf & vbCrLf
                sbdHTMLOrderEmail.Replace("[shippingname]", Server.HtmlEncode(.FullName))
                sbdHTMLOrderEmail.Replace("[shippingstreetaddress]", Server.HtmlEncode(.StreetAddress))
                sbdHTMLOrderEmail.Replace("[shippingtowncity]", Server.HtmlEncode(.TownCity))
                sbdHTMLOrderEmail.Replace("[shippingcounty]", Server.HtmlEncode(.County))
                sbdHTMLOrderEmail.Replace("[shippingpostcode]", Server.HtmlEncode(.Postcode))
                sbdHTMLOrderEmail.Replace("[shippingcountry]", Server.HtmlEncode(.Country.Name))
                sbdHTMLOrderEmail.Replace("[shippingphone]", Server.HtmlEncode(.Phone))
                If Not String.IsNullOrEmpty(.Company) Then
                    sbdHTMLOrderEmail.Replace("[shippingcompany]", Server.HtmlEncode(.Company))
                Else
                    sbdHTMLOrderEmail.Replace("[shippingcompany]<br />", "")
                    sbdHTMLOrderEmail.Replace("[shippingcompany]", "")
                End If
            End With
        Else
            With UC_ShippingAddress.SelectedAddress
                strShippingAddressEmailText = " " & .FullName & vbCrLf & " " & .Company & vbCrLf &
                                          " " & .StreetAddress & vbCrLf & " " & .TownCity & vbCrLf &
                                          " " & .County & vbCrLf & " " & .Postcode & vbCrLf &
                                          " " & .Country.Name & vbCrLf & vbCrLf
                sbdHTMLOrderEmail.Replace("[shippingname]", Server.HtmlEncode(.FullName))
                sbdHTMLOrderEmail.Replace("[shippingstreetaddress]", Server.HtmlEncode(.StreetAddress))
                sbdHTMLOrderEmail.Replace("[shippingtowncity]", Server.HtmlEncode(.TownCity))
                sbdHTMLOrderEmail.Replace("[shippingcounty]", Server.HtmlEncode(.County))
                sbdHTMLOrderEmail.Replace("[shippingpostcode]", Server.HtmlEncode(.Postcode))
                sbdHTMLOrderEmail.Replace("[shippingcountry]", Server.HtmlEncode(.Country.Name))
                sbdHTMLOrderEmail.Replace("[shippingphone]", Server.HtmlEncode(.Phone))
                If Not String.IsNullOrEmpty(.Company) Then
                    sbdHTMLOrderEmail.Replace("[shippingcompany]", Server.HtmlEncode(.Company))
                Else
                    sbdHTMLOrderEmail.Replace("[shippingcompany]", "")
                End If
            End With
        End If

        sbdBodyText.Append(strShippingAddressEmailText & GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker") & vbCrLf)

        'Comments and additional info
        sbdHTMLOrderEmail.Replace("[ordercomments]", "")

        sbdBodyText.Append(" " & GetGlobalResourceObject("Email", "EmailText_OrderTime2") & ": " & CkartrisDisplayFunctions.NowOffset & vbCrLf)
        sbdBodyText.Append(" " & GetGlobalResourceObject("Email", "EmailText_IPAddress") & ": " & CkartrisEnvironment.GetClientIPAddress() & vbCrLf)
        sbdBodyText.Append(" " & Request.ServerVariables("HTTP_USER_AGENT") & vbCrLf)
        If blnUseHTMLOrderEmail Then
            sbdHTMLOrderEmail.Replace("[nowoffset]", CkartrisDisplayFunctions.NowOffset)
            sbdHTMLOrderEmail.Replace("[customerip]", CkartrisEnvironment.GetClientIPAddress())
            sbdHTMLOrderEmail.Replace("[customeruseragent]", Request.ServerVariables("HTTP_USER_AGENT"))
            sbdHTMLOrderEmail.Replace("[webshopurl]", CkartrisBLL.WebShopURL)
            sbdHTMLOrderEmail.Replace("[websitename]", Server.HtmlEncode(GetGlobalResourceObject("Kartris", "Config_Webshopname")))
        End If

        sbdBodyText.Insert(0, sbdBasketItems.ToString)

        arrBasketItems = UC_BasketMain.GetBasketItems
        If Not (arrBasketItems Is Nothing) Then
            Dim BasketItem As New BasketItem
            'final check if basket items are still there
            If arrBasketItems.Count = 0 Then
                CkartrisFormatErrors.LogError("Basket items were lost in the middle of saving this order! Customer redirected to main Basket page." & vbCrLf &
                                                      "Details: " & sbdBodyText.ToString)
                Response.Redirect("~/Basket.aspx")
            End If

            'Get customer discount, we need this to decide whether to mark items
            'exempt from it
            Dim BSKT_CustomerDiscount As Double = 0
            Try
                BSKT_CustomerDiscount = BasketBLL.GetCustomerDiscount(lnkOrderCustomerID.Text)
            Catch ex As Exception
                'New user, just defaults to zero as no customer discount in this case
            End Try


            'We need to mark items that are exempt from customer discounts
            Dim strMark As String = ""
            Dim blnHasExemptCustomerDiscountItems As Boolean = False

            'Loop through basket items
            For Each Item As Kartris.BasketItem In arrBasketItems
                With Item
                    Dim strCustomControlName As String = objObjectConfigBLL.GetValue("K:product.customcontrolname", Item.ProductID)
                    Dim strCustomText As String = ""

                    Dim sbdOptionText As New StringBuilder("")
                    If Not String.IsNullOrEmpty(.OptionText) Then sbdOptionText.Append(vbCrLf & " " & .OptionText().Replace("<br>", vbCrLf & " ").Replace("<br />", vbCrLf & " "))

                    'Set string to blank or **, to mark items exempt from customer discount
                    If .ExcludeFromCustomerDiscount And BSKT_CustomerDiscount > 0 Then
                        strMark = " **"
                        blnHasExemptCustomerDiscountItems = True
                    Else
                        strMark = ""
                    End If

                    'Append line for this item
                    sbdBasketItems.AppendLine(
                                        GetItemEmailText(.Quantity & " x " & .ProductName & strMark, .VersionName & " (" & .CodeNumber & ")" &
                                                         sbdOptionText.ToString, .ExTax, .IncTax, .TaxAmount, .ComputedTaxRate))

                    If .CustomText <> "" AndAlso String.IsNullOrEmpty(strCustomControlName) Then
                        'Add custom text to mail
                        strCustomText = " [ " & .CustomText & " ]" & vbCrLf
                        sbdBasketItems.Append(strCustomText)
                    End If
                    If blnUseHTMLOrderEmail Then
                        'this line builds up the individual rows of the order contents table in the HTML email
                        sbdHTMLOrderBasket.AppendLine(GetHTMLEmailRowText(.Quantity & " x " & .ProductName & strMark, .VersionName & " (" & .CodeNumber & ") " &
                                                         sbdOptionText.ToString & strCustomText, .ExTax, .IncTax, .TaxAmount, .ComputedTaxRate, 0, .VersionID, .ProductID))
                    End If
                End With
            Next

            'Now we know if there are customer discount exempt items, can replace
            '[customerdiscountexempttext] which was inserted with the customer discount
            'line further above.
            If blnHasExemptCustomerDiscountItems Then
                sbdBodyText.Replace("[customerdiscountexempttext]", GetGlobalResourceObject("Basket", "ContentText_SomeItemsExcludedFromDiscount"))
                sbdHTMLOrderContents.Replace("[customerdiscountexempttext]", GetGlobalResourceObject("Basket", "ContentText_SomeItemsExcludedFromDiscount"))
            Else
                sbdBodyText.Replace("[customerdiscountexempttext]", "")
                sbdHTMLOrderContents.Replace("[customerdiscountexempttext]", "")
            End If
        End If

        sbdBodyText.Insert(0, sbdBasketItems.ToString)

        If blnUseHTMLOrderEmail Then
            'build up the table and the header tags, insert basket contents
            sbdHTMLOrderContents.Insert(0, "<table id=""orderitems""><thead><tr>" & vbCrLf &
                                                "<th class=""col1"">" & GetGlobalResourceObject("Kartris", "ContentText_Item") & "</th>" & vbCrLf &
                                                "<th class=""col2"">" & GetGlobalResourceObject("Kartris", "ContentText_Price") & "</th></thead><tbody>" & vbCrLf &
                                                sbdHTMLOrderBasket.ToString)
            'finally close the order contents HTML table
            sbdHTMLOrderContents.Append("</tbody></table>")
            'and append the order contents to the main HTML email
            sbdHTMLOrderEmail.Replace("[ordercontents]", sbdHTMLOrderContents.ToString)
        End If

        'check if shippingdestinationid is initialized, if not then reload checkout page
        If UC_BasketMain.ShippingDestinationID = 0 Then
            CkartrisFormatErrors.LogError("Basket was reset in edit order (back end). Shipping info lost." & vbCrLf & "BasketView Shipping Destination ID: " &
                                                      UC_BasketMain.ShippingDestinationID & vbCrLf)
            'Response.Redirect("~/Checkout.aspx")
        End If


        'Decide which email version to use
        If blnUseHTMLOrderEmail Then

            'Last few replacements
            strOrderDetails = sbdHTMLOrderEmail.ToString

            strOrderDetails = strOrderDetails.Replace("[storeowneremailheader]", "")
            strOrderDetails = strOrderDetails.Replace("[poofflinepaymentdetails]", "")
            strOrderDetails = strOrderDetails.Replace("[bitcoinpaymentdetails]", "")
        Else
            strOrderDetails = sbdBodyText.ToString
        End If

        'show original order ID
        strOrderDetails = strOrderDetails.Replace("[originalorderid]", intO_ID)

        'Create the order record
        Dim intNewOrderID As Integer = objOrdersBLL._CloneAndCancel(intO_ID, strOrderDetails, UC_BillingAddress.SelectedAddress, UC_ShippingAddress.SelectedAddress,
                                                                 chkSameShippingAsBilling.Checked, chkOrderSent.Checked, chkOrderInvoiced.Checked, chkOrderPaid.Checked,
                                                                 chkOrderShipped.Checked, objBasket, arrBasketItems,
                                                                UC_BasketMain.SelectedShippingMethod, txtOrderNotes.Text, numGatewayTotalPrice, txtOrderPONumber.Text,
                                                                strPromotionDescription, CUR_ID, chkSendOrderUpdateEmail.Checked)

        'Now we have new order ID, let's put this into the email
        strOrderDetails = strOrderDetails.Replace("[orderid]", intNewOrderID)

        'Send email of new order to the customer
        Dim strFromEmail As String = LanguagesBLL.GetEmailFrom(CInt(ddlOrderLanguage.SelectedValue))
        SendEmail(strFromEmail, txtOrderCustomerEmail.Text, GetGlobalResourceObject("Email", "Config_Subjectline") & " (#" & intNewOrderID & ")", strOrderDetails, , , , , blnUseHTMLOrderEmail)

        'Email to store owner
        SendEmail(strFromEmail, LanguagesBLL.GetEmailTo(1), GetGlobalResourceObject("Email", "Config_Subjectline2") & " (#" & intNewOrderID & ")", strOrderDetails, , , , , blnUseHTMLOrderEmail)

        'if we got a new order id then that means the order was successfully cloned and cancelled - lets now redirect the user to the new order details page
        If intNewOrderID > 0 Then
            If chkOrderPaid.Checked Then
                Try

                    UC_BasketMain.EmptyBasket_Click(Nothing, Nothing)
                    LoadBasket(True)

                    objBasket = Session("Basket")

                    UC_BasketMain.EmptyBasket_Click(Nothing, Nothing)
                Catch ex As Exception

                End Try
            End If

            objOrder = New Kartris.Interfaces.objOrder
            'Create the Order object and fill in the property values.
            objOrder.ID = intNewOrderID
            objOrder.Description = GetGlobalResourceObject("Kartris", "Config_OrderDescription")
            objOrder.Amount = CurrenciesBLL.FormatCurrencyPrice(CUR_ID, numGatewayTotalPrice, False)
            objOrder.ShippingPrice = CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.ShippingPrice.IncTax, False)
            objOrder.OrderHandlingPrice = CurrenciesBLL.FormatCurrencyPrice(CUR_ID, objBasket.OrderHandlingPrice.IncTax, False)

            With UC_BillingAddress.SelectedAddress
                objOrder.Billing.Name = .FullName
                objOrder.Billing.StreetAddress = .StreetAddress
                objOrder.Billing.TownCity = .TownCity
                objOrder.Billing.CountyState = .County
                objOrder.Billing.CountryName = .Country.Name
                objOrder.Billing.Postcode = .Postcode
                objOrder.Billing.Phone = .Phone
                objOrder.Billing.CountryIsoCode = .Country.IsoCode
                objOrder.Billing.Company = .Company
            End With

            If chkSameShippingAsBilling.Checked Then
                objOrder.SameShippingAsBilling = True
            Else
                With UC_ShippingAddress.SelectedAddress
                    objOrder.Shipping.Name = .FullName
                    objOrder.Shipping.StreetAddress = .StreetAddress
                    objOrder.Shipping.TownCity = .TownCity
                    objOrder.Shipping.CountyState = .County
                    objOrder.Shipping.CountryName = .Country.Name
                    objOrder.Shipping.Postcode = .Postcode
                    objOrder.Shipping.Phone = .Phone
                    objOrder.Shipping.CountryIsoCode = .Country.IsoCode
                    objOrder.Shipping.Company = .Company
                End With
            End If
            objOrder.CustomerIPAddress = Request.ServerVariables("REMOTE_HOST")
            objOrder.CustomerEmail = txtOrderCustomerEmail.Text

            'update data field with serialized order and basket objects and selected shipping method id - this allows us to edit this order later if needed
            objOrdersBLL.DataUpdate(intNewOrderID, Payment.Serialize(objOrder) & "|||" & Payment.Serialize(objBasket) & "|||" & UC_BasketMain.SelectedShippingID)

            Response.Redirect("_ModifyOrderStatus.aspx?OrderID=" & intNewOrderID & "&cloned=y")
        End If
    End Sub

    ''' <summary>
    ''' Checks autocomplete data
    ''' </summary>
    ''' <remarks></remarks>
    Function CheckAutoCompleteData() As Long
        Dim objVersionsBLL As New VersionsBLL
        Dim strAutoCompleteText As String = ""
        strAutoCompleteText = _UC_AutoComplete_Item.GetText
        If strAutoCompleteText <> "" AndAlso strAutoCompleteText.Contains("(") _
                AndAlso strAutoCompleteText.Contains(")") Then
            Try
                Dim numItemID As Long = CLng(Mid(strAutoCompleteText, strAutoCompleteText.LastIndexOf("(") + 2, strAutoCompleteText.LastIndexOf(")") - strAutoCompleteText.LastIndexOf("(") - 1))
                Dim strItemName As String = ""
                strItemName = objVersionsBLL._GetNameByVersionID(numItemID, Session("_LANG"))

                If strItemName Is Nothing Then
                    _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, GetGlobalResourceObject("_Kartris", "ContentText_InvalidValue"))
                    Return 0
                End If
                Return numItemID
            Catch ex As Exception
                _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, GetGlobalResourceObject("_Kartris", "ContentText_InvalidValue"))
                Return 0
            End Try
        Else
            _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, GetGlobalResourceObject("_Kartris", "ContentText_InvalidValue"))
            Return 0
        End If
    End Function

    ''' <summary>
    ''' This even fired when the selection from the options popup has been saved
    ''' </summary>
    Protected Sub _UC_OptionsPopup_OptionsSelected(ByVal strOptions As String, ByVal numVersionID As Integer) Handles _UC_OptionsPopup.OptionsSelected
        If Not String.IsNullOrEmpty(litOptionsVersion.Text) AndAlso litOptionsVersion.Text = numVersionID Then
            _UC_OptionsPopup.ClearIDs() '' reset product id and version id for the options popup
            litOptionsVersion.Text = Nothing
            Dim objBasket As Kartris.Basket = UC_BasketMain.GetBasket
            Dim sessionID As Long = Session("SessionID")
            BasketBLL.AddNewBasketValue(objBasket.BasketItems, BasketBLL.BASKET_PARENTS.BASKET, sessionID, numVersionID, 1, "", strOptions)
            LoadBasket()
        End If
    End Sub

    ''' <summary>
    ''' Create a link to customer, useful if need to edit
    ''' details before placing a new order
    ''' </summary>
    Public Function FormatCustomerLink(ByVal numCustomerID As Integer) As String
        Return "~/Admin/_ModifyCustomerStatus.aspx?CustomerID=" & numCustomerID
    End Function
End Class
