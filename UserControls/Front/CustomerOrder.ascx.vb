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
Imports CkartrisDataManipulation
Imports KartSettingsManager

Partial Class UserControls_Front_CustomerOrder
    Inherits System.Web.UI.UserControl

    Protected APP_PricesIncTax, APP_ShowTaxDisplay, blnHasExemptCustomerDiscountItems As Boolean
    Private objBasket As New Kartris.Basket
    Private numTaxDue, numTotalPriceExTax, numTotalPriceIncTax, numCouponDiscount, numCustomerDiscount, numShipping, numOrderHandlingCharge, numTotal As Double
    Private numDiscountPercentage, numPromotionDiscountTotal, numCouponDiscountTotal, CP_DiscountValue As Double
    Private strCouponCode, CP_DiscountType, CP_CouponCode As String
    Private numShippingPriceIncTax, numShippingPriceExTax, numOrderHandlingPriceIncTax, numOrderHandlingPriceExTax, numFinalTotalPriceInTaxGateway As Double
    Private numCurrencyIDGateway, numCurrencyID As Double
    Private _OrderID As Long
    Private _CustomerID As Long
    Private _ShowOrderSummary As Boolean

    Public Property OrderID() As Long
        Get
            Return _OrderID
        End Get
        Set(ByVal value As Long)
            _OrderID = value
        End Set
    End Property

    Public Property CustomerID() As Long
        Get
            Return _CustomerID
        End Get
        Set(ByVal value As Long)
            _CustomerID = value
        End Set
    End Property

    Public Property ShowOrderSummary() As Boolean
        Get
            Return _ShowOrderSummary
        End Get
        Set(ByVal value As Boolean)
            _ShowOrderSummary = value
        End Set
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim tblBasket As Data.DataTable

        phdViewOrder.Visible = ShowOrderSummary

        Dim objBasketBLL As New BasketBLL
        tblBasket = objBasketBLL.GetCustomerOrderDetails(OrderID)

        If tblBasket.Rows.Count > 0 Then
            Dim datOrderDate, datLastModified As DateTime

            datOrderDate = FixNullFromDB(tblBasket.Rows(0).Item("O_Date"))
            datLastModified = FixNullFromDB(tblBasket.Rows(0).Item("O_LastModified"))
            numTotal = FixNullFromDB(tblBasket.Rows(0).Item("O_TotalPrice"))
            numTaxDue = FixNullFromDB(tblBasket.Rows(0).Item("O_TaxDue"))
            numPromotionDiscountTotal = FixNullFromDB(tblBasket.Rows(0).Item("O_PromotionDiscountTotal"))
            strCouponCode = FixNullFromDB(tblBasket.Rows(0).Item("O_CouponCode")) & ""
            numCouponDiscountTotal = FixNullFromDB(tblBasket.Rows(0).Item("O_CouponDiscountTotal"))
            numDiscountPercentage = FixNullFromDB(tblBasket.Rows(0).Item("O_DiscountPercentage"))
            numShippingPriceExTax = FixNullFromDB(tblBasket.Rows(0).Item("O_ShippingPrice"))
            numShippingPriceIncTax = FixNullFromDB(tblBasket.Rows(0).Item("O_ShippingPrice")) + FixNullFromDB(tblBasket.Rows(0).Item("O_ShippingTax"))
            numOrderHandlingPriceExTax = FixNullFromDB(tblBasket.Rows(0).Item("O_OrderHandlingCharge"))
            numOrderHandlingPriceIncTax = numOrderHandlingPriceExTax + FixNullFromDB(tblBasket.Rows(0).Item("O_OrderHandlingChargeTax"))


            numCurrencyIDGateway = FixNullFromDB(tblBasket.Rows(0).Item("O_CurrencyIDGateway"))
            Dim blnBitcoinGateway As Boolean = (CurrenciesBLL.CurrencyCode(numCurrencyIDGateway).ToLower = "btc")
            If blnBitcoinGateway Then
                numFinalTotalPriceInTaxGateway = Math.Round(tblBasket.Rows(0).Item("O_TotalPriceGateway"), 8)
            Else
                numFinalTotalPriceInTaxGateway = Math.Round(tblBasket.Rows(0).Item("O_TotalPriceGateway"), 2)
            End If

            numCurrencyID = FixNullFromDB(tblBasket.Rows(0).Item("O_CurrencyID"))

            'show tax if config says so
            APP_ShowTaxDisplay = (LCase(GetKartConfig("frontend.display.showtax")) = "y") Or (LCase(GetKartConfig("frontend.display.showtax")) = "c")

            'check if the prices in the order are inctax or extax
            APP_PricesIncTax = tblBasket.Rows(0).Item("O_PricesIncTax") = True

            If strCouponCode <> "" Then
                Dim tblCoupon As Data.DataTable
                tblCoupon = objBasketBLL.GetCouponData(strCouponCode)
                If tblCoupon.Rows.Count > 0 Then
                    CP_DiscountValue = tblCoupon.Rows(0).Item("CP_DiscountValue")
                    CP_DiscountType = tblCoupon.Rows(0).Item("CP_DiscountType") & ""
                    If CP_DiscountType = "f" Then CP_DiscountValue = numCouponDiscountTotal
                    CP_CouponCode = tblCoupon.Rows(0).Item("CP_CouponCode") & ""
                End If
            End If

            litOrderID.Text = OrderID
            litOrderDate.Text = datOrderDate.ToString("dd MMM yy")
            litTotalPrice.Text = CurrenciesBLL.FormatCurrencyPrice(CurrenciesBLL.GetDefaultCurrency(), numTotal)
            litLastModified.Text = datLastModified.ToString("dd MMM yy, hh:mm")

            imgSent.ImageUrl = IIf(tblBasket.Rows(0).Item("O_Sent"), "~/Images/Kartris/tick.gif", "~/Images/Kartris/tick_empty.gif")
            imgInvoiced.ImageUrl = IIf(tblBasket.Rows(0).Item("O_Invoiced"), "~/Images/Kartris/tick.gif", "~/Images/Kartris/tick_empty.gif")
            imgPaid.ImageUrl = IIf(tblBasket.Rows(0).Item("O_Paid"), "~/Images/Kartris/tick.gif", "~/Images/Kartris/tick_empty.gif")
            imgShipped.ImageUrl = IIf(tblBasket.Rows(0).Item("O_Shipped"), "~/Images/Kartris/tick.gif", "~/Images/Kartris/tick_empty.gif")
            imgCancelled.ImageUrl = IIf(tblBasket.Rows(0).Item("O_Cancelled"), "~/Images/Kartris/tick.gif", "~/Images/Kartris/tick_empty.gif")

            If Len(tblBasket.Rows(0).Item("O_Status")) > 0 Then
                Dim strOrderStatus As String = tblBasket.Rows(0).Item("O_Status")

                'We don't want to show it if still includes the XML 
                If strOrderStatus.ToLower.Contains("<basket") Then
                    'Is XML text, ignore it
                Else
                    litOrderStatus.Text = strOrderStatus & ""
                    phdStatus.Visible = True
                End If
            End If

            'Sort the basket items so items excluded from customer
            'discount are at the bottom
            Dim dvwSorting As DataView = tblBasket.DefaultView
            dvwSorting.Sort = "IR_ExcludeFromCustomerDiscount ASC"
            Dim tblBasketSorted As DataTable = dvwSorting.ToTable()

            'Bind the repeater to the new sorted datatable
            rptBasket.DataSource = tblBasketSorted
            rptBasket.DataBind()

        End If


    End Sub

    Protected Sub rptBasket_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.RepeaterItemEventArgs) Handles rptBasket.ItemDataBound
        Dim strVersionCode As String
        Dim numPricePerItem, numTaxPerItem, numQuantity As Single
        Dim numRowPriceExTax, numRowPriceIncTax As Double
        Dim numCurrencyRoundPlaces As Integer = BasketBLL.CurrencyRoundNumber
        If e.Item.ItemType = ListItemType.Item OrElse e.Item.ItemType = ListItemType.AlternatingItem Then
            strVersionCode = e.Item.DataItem("IR_VersionCode")
            If e.Item.DataItem("IR_OptionsText") = "" Then strVersionCode = strVersionCode & "<br />" & e.Item.DataItem("IR_OptionsText")
            CType(e.Item.FindControl("litVersionCode"), Literal).Text = strVersionCode

            numPricePerItem = e.Item.DataItem("IR_PricePerItem")
            numTaxPerItem = e.Item.DataItem("IR_TaxPerItem")
            numQuantity = e.Item.DataItem("IR_Quantity")

            If APP_PricesIncTax Then
                CType(e.Item.FindControl("phdIncTax"), PlaceHolder).Visible = True
                If APP_ShowTaxDisplay Then
                    CType(e.Item.FindControl("phdIncTaxDisplay"), PlaceHolder).Visible = True
                    CType(e.Item.FindControl("litIncTaxPrice1"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numPricePerItem - numTaxPerItem)
                    CType(e.Item.FindControl("litIncTaxPrice2"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numPricePerItem)
                Else
                    CType(e.Item.FindControl("phdIncTaxHide"), PlaceHolder).Visible = True
                    CType(e.Item.FindControl("litIncTaxPrice"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numPricePerItem)
                End If
                CType(e.Item.FindControl("litIncTaxQty"), Literal).Text = numQuantity
                CType(e.Item.FindControl("litIncTaxTotal"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numPricePerItem * numQuantity)
            Else
                CType(e.Item.FindControl("phdExTax"), PlaceHolder).Visible = True
                If APP_ShowTaxDisplay Then
                    CType(e.Item.FindControl("phdExTaxDisplay"), PlaceHolder).Visible = True
                    CType(e.Item.FindControl("litExTaxPrice1"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numPricePerItem)
                    CType(e.Item.FindControl("litExTaxPrice2"), Literal).Text = Math.Round(100 * numTaxPerItem, numCurrencyRoundPlaces) & "%"
                Else
                    CType(e.Item.FindControl("phdExTaxHide"), PlaceHolder).Visible = True
                    CType(e.Item.FindControl("litExTaxPrice"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numPricePerItem)
                End If
                CType(e.Item.FindControl("litExTaxQty"), Literal).Text = numQuantity
                CType(e.Item.FindControl("litExTaxTotal"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, 0.0000001 + (numPricePerItem * numQuantity * (1 + numTaxPerItem)))
            End If

            If APP_PricesIncTax Then
                'INC TAX - Price of row inc tax (row = items x qty)
                numRowPriceIncTax = numPricePerItem * numQuantity
                numRowPriceExTax = Math.Round((numQuantity * (numPricePerItem - numTaxPerItem)) - 0.0000001, numCurrencyRoundPlaces)
                numTotalPriceExTax = numTotalPriceExTax + numRowPriceExTax
                numTotalPriceIncTax = numTotalPriceIncTax + numRowPriceIncTax
            Else
                'EX TAX  - Price of row ex tax (row = items x qty)
                numRowPriceExTax = Math.Round(numPricePerItem * numQuantity, numCurrencyRoundPlaces)
                numRowPriceIncTax = Math.Round(0.0000001 + (numRowPriceExTax * (1 + numTaxPerItem)), numCurrencyRoundPlaces)
                numTotalPriceExTax = numTotalPriceExTax + numRowPriceExTax
                numTotalPriceIncTax = numTotalPriceIncTax + numRowPriceIncTax
            End If

            If e.Item.DataItem("IR_ExcludeFromCustomerDiscount") Then
                blnHasExemptCustomerDiscountItems = True
            End If

        ElseIf e.Item.ItemType = ListItemType.Footer Then

            'shipping and order handling
            If numTaxDue > 0 Then
                CType(e.Item.FindControl("litTotOrderValue"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numTotalPriceIncTax)
                CType(e.Item.FindControl("litShipping"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numShippingPriceIncTax)
                CType(e.Item.FindControl("litOrderHandlingCharge"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numOrderHandlingPriceIncTax)
            Else
                CType(e.Item.FindControl("litTotOrderValue"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numTotalPriceExTax)
                CType(e.Item.FindControl("litShipping"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numShippingPriceExTax)
                CType(e.Item.FindControl("litOrderHandlingCharge"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numOrderHandlingPriceExTax)
            End If

            'Get shipping method
            'Added v3.2002
            'We're going to try to get the shipping details from the XML copy of the order
            Try
                Dim objBasketBLL As New BasketBLL
                Dim tblBasket As Data.DataTable = objBasketBLL.GetCustomerOrderDetails(OrderID)
                If tblBasket.Rows.Count > 0 Then
                    CType(e.Item.FindControl("litShippingDesc"), Literal).Text = "(" & tblBasket.Rows(0).Item("O_ShippingMethod") & ")"
                End If
            Catch ex As Exception
                'Ex
            End Try

            'Promotions line
            If numPromotionDiscountTotal < 0 Then
                CType(e.Item.FindControl("phdPromotion"), PlaceHolder).Visible = True
                CType(e.Item.FindControl("litPromotionDiscount"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numPromotionDiscountTotal)
            End If

            'Coupon line
            If strCouponCode <> "" And LCase(CP_DiscountType) <> "t" Then
                CType(e.Item.FindControl("phdCoupon"), PlaceHolder).Visible = True
                If LCase(CP_DiscountType) = "p" Then
                    CType(e.Item.FindControl("litCouponDiscount"), Literal).Text = CP_DiscountValue & "%"
                Else
                    CType(e.Item.FindControl("litCouponDiscount"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, CP_DiscountValue)
                End If
            End If

            'Discount percentage line
            If numDiscountPercentage <> 0 Then
                'check last item, if it is excluded from customer discount
                'then we need to show explanation for customer discount
                CType(e.Item.FindControl("phdCustomer"), PlaceHolder).Visible = True
                CType(e.Item.FindControl("litCustomerDiscount"), Literal).Text = numDiscountPercentage & "%"

                If blnHasExemptCustomerDiscountItems Then
                    CType(e.Item.FindControl("phdSomeItemsExcluded"), PlaceHolder).Visible = True
                End If
            End If

            'Total
            CType(e.Item.FindControl("litTotal"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyID, numTotal)

            If numCurrencyIDGateway <> numCurrencyID Then
                CType(e.Item.FindControl("phdTotalGateway"), PlaceHolder).Visible = True
                CType(e.Item.FindControl("litTotalGateway"), Literal).Text = CurrenciesBLL.FormatCurrencyPrice(numCurrencyIDGateway, numFinalTotalPriceInTaxGateway)
            End If

        End If

    End Sub


End Class
