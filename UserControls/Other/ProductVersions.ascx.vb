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
Imports CkartrisTaxes
Imports CkartrisDataManipulation
Imports CkartrisImages
Imports KartSettingsManager

''' <summary>
''' Used in the ProductView.ascx and ProductTemplateExtended.ascx, to view the available versions
''' of the current Product.
''' </summary>
''' <remarks></remarks>
Partial Class ProductVersions
    Inherits System.Web.UI.UserControl

    Private _ProductID As Integer = -1
    Private _LanguageID As Short = -1
    Private _ViewType As Char
    Private numVersionID As Int64 = -1

    Private c_blnHasPrices As Boolean = True
    Private c_blnShowMediaGallery As Boolean = True

    Protected BasketItem_VersionID As Long = 0
    Private Shared BasketItemInfo As String = ""

    Public ReadOnly Property ProductID() As Integer
        Get
            Return _ProductID
        End Get
    End Property

    Public ReadOnly Property HasPrice() As Boolean
        Get
            Return c_blnHasPrices
        End Get
    End Property

    Public Property ShowMediaGallery() As Boolean
        Get
            Return c_blnShowMediaGallery
        End Get
        Set(ByVal value As Boolean)
            c_blnShowMediaGallery = value
        End Set
    End Property

    ''' <summary>
    ''' Page load
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim objVersionsBLL As New VersionsBLL
        ddlVersionImages.Visible = False
        Try
            phdDropdownCustomizable.Visible = objVersionsBLL.IsVersionCustomizable(CLng(ddlName_DropDown.SelectedValue))
        Catch
            'do nothing
        End Try
        If Request.QueryString("strOptions") & "" = "" Then Session("BasketItemInfo") = ""
    End Sub

    ''' <summary>
    ''' Returns boolean True if we need to hide add-to-basket button
    ''' </summary>
    ''' <remarks></remarks>
    Public Function CheckHideAddButton() As Boolean
        If KartSettingsManager.GetKartConfig("frontend.users.access") = "partial" And Not HttpContext.Current.User.Identity.IsAuthenticated Then
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Loads/Creates the Display Type of the product version.
    ''' </summary>
    ''' <param name="pProductID"></param>
    ''' <param name="pLanguageID"></param>
    ''' <param name="pViewType"></param>
    ''' <remarks></remarks>
    Public Sub LoadProductVersions(ByVal pProductID As Integer, ByVal pLanguageID As Short, ByVal pViewType As Char)

        _ProductID = pProductID
        _LanguageID = pLanguageID

        '' If the ProductID is -1, that means no Product was selected
        '' the viwEmpty is activated and Exit.
        If _ProductID = -1 Then mvwVersion.SetActiveView(viwEmpty) : Exit Sub

        '' If the viewType of the Versions is d(default), then will get the default
        '' from the CONFIG Settings
        If pViewType = "d" Then pViewType = GetKartConfig("frontend.versions.display.default")

        _ViewType = pViewType

        Dim numCGroupID As Short = 0
        If HttpContext.Current.User.Identity.IsAuthenticated Then
            numCGroupID = CShort(DirectCast(Page, PageBaseClass).CurrentLoggedUser.CustomerGroupID)
        End If

        Dim tblVersions As DataTable
        Dim objVersionsBLL As New VersionsBLL
        tblVersions = objVersionsBLL.GetByProduct(_ProductID, _LanguageID, numCGroupID)

        If tblVersions.Rows.Count = 0 Then mvwVersion.SetActiveView(viwError) : Exit Sub

        'Load the custom control for the product
        Dim objObjectConfigBLL As New ObjectConfigBLL
        Dim strCustomControlName As String = objObjectConfigBLL.GetValue("K:product.customcontrolname", _ProductID)
        If Not String.IsNullOrEmpty(strCustomControlName) Then
            mvwVersion.SetActiveView(viwCustomVersion)
            Dim UC_CustomControl As KartrisClasses.CustomProductControl = LoadControl("~/UserControls/Custom/" & strCustomControlName)
            If UC_CustomControl IsNot Nothing Then
                UC_CustomControl.ID = "UC_CustomControl"
                phdCustomControl.Controls.Add(UC_CustomControl)
            End If

            If Not IsPostBack Then
                ddlCustomVersionQuantity.Items.Clear()
                'Create custom control quantity dropdown values based on config setting
                For numCounter = 1 To CInt(KartSettingsManager.GetKartConfig("frontend.basket.addtobasketdropdown.max"))
                    ddlCustomVersionQuantity.Items.Add(numCounter.ToString)
                Next
                'Get the first available version for the product and set it as the custom product version ID
                litHiddenV_ID.Text = FixNullFromDB(tblVersions.Rows(0)("V_ID"))
            End If
            'Exit Sub as this is a custom product and we don't want to load a different view from the code below
            Exit Sub
        End If

        '' Depending on the ViewType, the corresponding viewControl will be activated,
        ''  and the corresponding repeater as well.
        Select Case pViewType
            Case "l" 'single version product
                mvwVersion.SetActiveView(PrepareSelectView(tblVersions))
            Case "r" 'tabular rows
                mvwVersion.SetActiveView(PrepareRowsView(tblVersions))
            Case "p" 'dropdown views
                If Not Page.IsPostBack Then
                    mvwVersion.SetActiveView(PrepareDropDownView(tblVersions))
                End If
            Case "o", "g" 'options
                c_blnHasPrices = False
                mvwVersion.SetActiveView(PrepareOptionsView(tblVersions))
            Case Else
                c_blnHasPrices = False
                mvwVersion.SetActiveView(PrepareSelectView(tblVersions))
        End Select
    End Sub

    ''' <summary>
    ''' Prepares the 'rows' version display
    ''' </summary>
    ''' <remarks></remarks>
    Private Function PrepareRowsView(ByVal ptblVersions As DataTable) As View
        '' Will move around all the resulted versions' rows and Convert the Pricing Currency.
        Dim numPrice As Single = 0.0F, numRRP As Single = 0.0F
        For Each drwVersion As DataRow In ptblVersions.Rows
            ''//
            drwVersion("V_Price") = GetPriceWithGroupDiscount(drwVersion("V_ID"), drwVersion("V_Price"))

            Dim drwCurrencies As DataRow() = GetCurrenciesFromCache().Select("CUR_ID = " & Session("CUR_ID"))
            Dim numCalculatedTax As Single = CalculateTax(Math.Round(CDbl(FixNullFromDB(drwVersion("V_Price"))), drwCurrencies(0)("CUR_RoundNumbers")),
              CDbl(FixNullFromDB(drwVersion("T_TaxRate"))))
            drwCurrencies = Nothing
            drwVersion("CalculatedTax") = CStr(CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), numCalculatedTax))

            numPrice = CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), CDbl(FixNullFromDB(drwVersion("V_Price"))))
            drwVersion("V_Price") = CStr(numPrice)

            numRRP = CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), CDbl(FixNullFromDB(drwVersion("V_RRP"))))
            drwVersion("V_RRP") = CStr(numRRP)
        Next

        '' Binds the DataTable to the Repeater.
        rptRows.DataSource = ptblVersions
        rptRows.DataBind()

        '' Returns the ID of the View Control to be activated.
        Return viwVersionRows
    End Function

    ''' <summary>
    ''' Prepares the 'dropdown' version display
    ''' </summary>
    ''' <remarks></remarks>
    Private Function PrepareDropDownView(ByVal ptblVersions As DataTable) As View

        '' Will move around all the resulted versions' rows and Convert the Pricing Currency.
        ''  and as this is the DropDownView .. every row will be added to the DropDownList.
        Dim numPrice As Single = 0.0F
        Dim numPrice2 As Single = 0.0F
        Dim intCounter As Integer = 0
        Dim intFirstInStock As Integer = -1 'we use this so if there are out of stock items, we always pre-select an in-stock one
        Dim strV_Name As String = ""
        Dim strV_Price As String = ""

        ddlName_DropDown.Items.Clear()
        Dim objObjectConfigBLL As New ObjectConfigBLL
        Dim blnIsCallForPrice As Boolean = (objObjectConfigBLL.GetValue("K:product.callforprice", _ProductID) = 1)

        If blnIsCallForPrice Then
            For Each drwVersion As DataRow In ptblVersions.Rows
                'we want to format fixed length strings for:
                ' - V_Name
                ' - out of stock
                strV_Name = drwVersion("V_Name")
                'Need to check if out of stock
                If drwVersion("V_Quantity") < 1.0F And drwVersion("V_QuantityWarnLevel") > 0.0F Then
                    'out of stock
                    ddlName_DropDown.Items.Add(New ListItem(strV_Name &
                     " [" &
                     GetGlobalResourceObject("Versions", "ContentText_AltOutOfStock") &
                     "]", drwVersion("V_ID")))
                    txtOutOfStockItems.Text &= "," & intCounter & ","
                Else
                    'available
                    If intFirstInStock = -1 Then intFirstInStock = intCounter
                    ddlName_DropDown.Items.Add(New ListItem(strV_Name, drwVersion("V_ID")))
                End If

                intCounter += 1
            Next
            'Need to disable the AutoPostBack
            ddlName_DropDown.AutoPostBack = False
            'Select the first in-stock item
            ddlName_DropDown.SelectedIndex = intFirstInStock
            'Hide add button and dropdown, show call for price
            phdOutOfStock3.Visible = False
            phdNotOutOfStock3.Visible = False
            phdCalForPrice3.Visible = True
        Else
            For Each drwVersion As DataRow In ptblVersions.Rows
                ''//
                drwVersion("V_Price") = GetPriceWithGroupDiscount(drwVersion("V_ID"), drwVersion("V_Price"))
                Dim drwCurrencies As DataRow() = GetCurrenciesFromCache().Select("CUR_ID = " & Session("CUR_ID"))
                Dim numCalculatedTax As Single = CalculateTax(Math.Round(CDbl(FixNullFromDB(drwVersion("V_Price"))), drwCurrencies(0)("CUR_RoundNumbers")),
                  CDbl(FixNullFromDB(drwVersion("T_TaxRate"))))

                numPrice = CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), CDbl(drwVersion("V_Price")))
                numPrice2 = CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), CDbl(numCalculatedTax))
                'we want to format fixed length strings for:
                ' - V_Name
                ' - V_Price
                ' - out of stock

                strV_Name = drwVersion("V_Name") & " -- "
                If KartSettingsManager.GetKartConfig("frontend.display.showtax") = "y" Then
                    'Show tax display
                    If KartSettingsManager.GetKartConfig("general.tax.pricesinctax") = "y" Then
                        'ExTax / IncTax
                        strV_Price = " " &
                        GetGlobalResourceObject("Kartris", "ContentText_ExTax") & " " &
                        CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice2, , False) & " -- " &
                        GetGlobalResourceObject("Kartris", "ContentText_IncTax") & " " &
                        CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice, , False)
                    Else
                        'litExTax / Tax%
                        strV_Price = " " &
                        GetGlobalResourceObject("Kartris", "ContentText_ExTax") & " " &
                        CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice, , False) & " -- " &
                        GetGlobalResourceObject("Kartris", "ContentText_Tax") & " " &
                        CkartrisDisplayFunctions.FixDecimal(drwVersion("T_TaxRate")) & "%"
                    End If
                Else
                    'Just show price
                    strV_Price = " " & CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice, , False)
                End If

                'Need to check if out of stock
                If drwVersion("V_Quantity") < 1.0F And drwVersion("V_QuantityWarnLevel") > 0.0F And KartSettingsManager.GetKartConfig("frontend.orders.allowpurchaseoutofstock") <> "y" Then
                    'out of stock
                    ddlName_DropDown.Items.Add(New ListItem(strV_Name &
                     CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice, , False) &
                     " [" &
                     GetGlobalResourceObject("Versions", "ContentText_AltOutOfStock") &
                     "]", drwVersion("V_ID")))
                    txtOutOfStockItems.Text &= "," & intCounter & ","
                Else
                    'available
                    If intFirstInStock = -1 Then intFirstInStock = intCounter
                    ddlName_DropDown.Items.Add(New ListItem(strV_Name & strV_Price, drwVersion("V_ID")))
                End If
                intCounter += 1
            Next
            ddlName_DropDown.AutoPostBack = True

            'Select the first in-stock item
            ddlName_DropDown.SelectedIndex = intFirstInStock

            'First version might not be available to add to basket...
            CheckIfHidingButtons()
        End If

        '' Returns the ID of the View Control to be activated.
        Return viwVersionDropDown

    End Function

    '''' <summary>
    '''' Handles the change of the dropdown selection
    '''' </summary>
    '''' <remarks></remarks>
    Protected Sub ddlName_DropDown_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlName_DropDown.SelectedIndexChanged

        CheckIfHidingButtons()

    End Sub

    '''' <summary>
    '''' Checks if need to hide 'add' button because item is 'call for price' or 'out of stock'
    '''' </summary>
    '''' <remarks></remarks>
    Sub CheckIfHidingButtons()

        'Handle out of stock stuff
        If txtOutOfStockItems.Text.Contains("," & ddlName_DropDown.SelectedIndex & ",") Then
            phdOutOfStock3.Visible = True
            phdNotOutOfStock3.Visible = False

            'Set the command argument for this item based
            'We use this for stock notifications popup
            Dim strVersionName As String = ddlName_DropDown.SelectedItem.Text
            strVersionName = strVersionName.Substring(0, Math.Max(strVersionName.IndexOf(" --"), 0))
            btnNotifyMe3.CommandArgument = FormatStockNotificationDetails(ddlName_DropDown.SelectedItem.Value, _ProductID, strVersionName, Request.RawUrl.ToString.ToLower, Session("LANG"))
        Else
            phdOutOfStock3.Visible = False
            phdNotOutOfStock3.Visible = True
            Dim objVersionsBLL As New VersionsBLL
            phdDropdownCustomizable.Visible = objVersionsBLL.IsVersionCustomizable(CLng(ddlName_DropDown.SelectedValue))

            'In multi-version dropdown, if the [T] for text customization is not
            'visible, then we don't need to show the AJAX customization popup, so
            'can activate the client side 'add to basket' one.
            If phdDropdownCustomizable.Visible = False Then
                btnAddVersions3.OnClientClick = "ShowAddToBasketPopup(" & KartSettingsManager.GetKartConfig("frontend.basket.behaviour") * 1000 & ")"
            Else
                btnAddVersions3.OnClientClick = Nothing
            End If
        End If

    End Sub

    ''' <summary>
    ''' Prepares the 'options' version display
    ''' </summary>
    ''' <remarks>by Paul</remarks>
    Private Function PrepareOptionsView(ByVal ptblVersions As DataTable) As View
        Dim objProductsBLL As New ProductsBLL
        Dim blnIncTax As Boolean = IIf(KartSettingsManager.GetKartConfig("general.tax.pricesinctax") = "y", True, False)
        Dim blnShowTax As Boolean = IIf(KartSettingsManager.GetKartConfig("frontend.display.showtax") = "y", True, False)
        'Added check now for _NumberOfCombinations, so we don't
        'use this setting if it's an options product, not a combinations
        'product
        Dim objObjectConfigBLL As New ObjectConfigBLL
        Dim blnUseCombinationPrice As Boolean = IIf(objObjectConfigBLL.GetValue("K:product.usecombinationprice", ProductID) = "1", True, False) And objProductsBLL._NumberOfCombinations(ProductID) > 0
        ptblVersions.Rows(0)("V_Price") = GetPriceWithGroupDiscount(ptblVersions.Rows(0)("V_ID"), ptblVersions.Rows(0)("V_Price"))

        ptblVersions.Rows(0)("V_Price") = CurrenciesBLL.ConvertCurrency(Session("CUR_ID"),
               CDbl(CStr(FixNullFromDB(ptblVersions.Rows(0)("V_Price")))))
        'base version price and tax rate
        Dim pPrice As Single = CDbl(CStr(FixNullFromDB(ptblVersions.Rows(0)("V_Price"))))
        'store base price and tax
        litPriceHidden.Text = CStr(FixNullFromDB(ptblVersions.Rows(0)("V_Price")))
        If ConfigurationManager.AppSettings("TaxRegime").ToLower <> "us" And ConfigurationManager.AppSettings("TaxRegime").ToLower <> "simple" Then litTaxRateHidden.Text = CStr(ptblVersions.Rows(0)("T_TaxRate"))

        'calculate display price
        PricePreview(pPrice)

        lblVID_Options.Text = CStr(FixNullFromDB(ptblVersions.Rows(0)("V_ID")))

        'Initializes/Loads the OptionsContainer UC to view the Options that are available for the Product.
        UC_OptionsContainer.InitializeOption(_ProductID, _LanguageID, blnUseCombinationPrice)

        If blnUseCombinationPrice Then
            'Call the function to check the selected combination price
            GetCombinationPrice()
        Else
            AddOptionsPrice(UC_OptionsContainer.GetSelectedPrice())
        End If

        ''// set addtobasket control's version id
        'If Not (IsPostBack) Then
        Dim strQuantityControl As String = LCase(objObjectConfigBLL.GetValue("K:product.addtobasketqty", ProductID))
        If Request.QueryString("strOptions") <> "" AndAlso (strQuantityControl = "dropdown" OrElse strQuantityControl = "textbox") Then

            If Session("BasketItemInfo") & "" <> "" Then
                Dim arrBasketItemInfo() As String = Split(Session("BasketItemInfo") & "", ";")
                If IsNumeric(lblVID_Options.Text) Then

                    BasketItem_VersionID = CLng(lblVID_Options.Text)
                    UC_AddToBasketQty4.VersionID = BasketItem_VersionID
                    UC_AddToBasketQty4.ItemEditVersionID = CDbl(arrBasketItemInfo(1))
                    UC_AddToBasketQty4.ItemEditQty = CDbl(arrBasketItemInfo(2))
                    btnAdd_Options.Text = GetGlobalResourceObject("_Kartris", "FormButton_Update")

                End If
            End If

        Else
            If IsNumeric(lblVID_Options.Text) Then UC_AddToBasketQty4.VersionID = CLng(lblVID_Options.Text)
            btnAdd_Options.Text = GetGlobalResourceObject("Products", "FormButton_Add")
        End If

        Dim objVersionsBLL As New VersionsBLL
        phdOptionsCustomizable.Visible = objVersionsBLL.IsVersionCustomizable(CLng(FixNullFromDB(ptblVersions.Rows(0)("V_ID"))))

        If UC_OptionsContainer.GetNoOfRows > 0 Then
            Return viwVersionOptions
        End If

        Return viwError
    End Function

    ''' <summary>
    ''' Prepares the 'select' version display
    ''' </summary>
    ''' <remarks></remarks>
    Private Function PrepareSelectView(ByVal ptblVersions As DataTable) As View

        '' Will move around all the resulted versions' rows and Convert the Pricing Currency.
        Dim numPrice As Single = 0.0F
        For Each drwVersion As DataRow In ptblVersions.Rows
            ''//
            drwVersion("V_Price") = GetPriceWithGroupDiscount(drwVersion("V_ID"), drwVersion("V_Price"))

            Dim drwCurrencies As DataRow() = GetCurrenciesFromCache().Select("CUR_ID = " & Session("CUR_ID"))
            Dim numCalculatedTax As Single = CalculateTax(Math.Round(CDbl(FixNullFromDB(drwVersion("V_Price"))), drwCurrencies(0)("CUR_RoundNumbers")),
             CDbl(FixNullFromDB(drwVersion("T_TaxRate"))))
            'Dim calculatedTax As Single = CalculateTax(CDbl(row("V_Price")), CDbl(FixNullFromDB(row("T_TaxRate"))))
            drwCurrencies = Nothing

            drwVersion("CalculatedTax") = CStr(CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), numCalculatedTax))

            numPrice = CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), CDbl(FixNullFromDB(drwVersion("V_Price"))))
            drwVersion("V_Price") = CStr(numPrice)
        Next

        '' Binds the DataTable to the Repeater.
        fvwPrice.DataSource = ptblVersions
        fvwPrice.DataBind()

        Return viwSelect

    End Function

    ''' <summary>
    ''' Runs for 'rows' display
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub rptRows_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.RepeaterItemEventArgs) Handles rptRows.ItemDataBound
        Dim objObjectConfigBLL As New ObjectConfigBLL
        For Each ctlItem As Control In e.Item.Controls
            Select Case ctlItem.ID

                Case "phdVersionImage"
                    e.Item.FindControl("UC_ImageViewer_Rows").Visible = True
                    Dim numVersionID As Integer
                    numVersionID = CLng(CType(e.Item.FindControl("lblVID_Rows"), Label).Text)

                    Dim UC_ImageView As New ImageViewer
                    UC_ImageView = CType(e.Item.FindControl("UC_ImageViewer_Rows"), ImageViewer)

                    UC_ImageView.CreateImageViewer(IMAGE_TYPE.enum_VersionImage,
                      numVersionID,
                      KartSettingsManager.GetKartConfig("frontend.display.images.thumb.height"),
                      KartSettingsManager.GetKartConfig("frontend.display.images.thumb.width"),
                      "",
                      _ProductID,
                      ImageViewer.SmallImagesType.enum_ImageButton)

                    'Hide whole image and container if no image available, otherwise can end up
                    'with small square visible if there is a border and background set for the 
                    'image holder in CSS
                    If UC_ImageView.FoundImage = False Then CType(e.Item.FindControl("phdVersionImage"), PlaceHolder).Visible = False

                Case "phdMediaGallery"
                    If Not c_blnShowMediaGallery Then
                        e.Item.FindControl("UC_VersionMediaGallery").Visible = False
                    End If

                Case "litResultedPrice_Rows"
                    ''Handle 'call for prices' - determine whether to
                    If objObjectConfigBLL.GetValue("K:product.callforprice", _ProductID) = 1 Then
                        CType(e.Item.FindControl("litResultedPrice_Rows"), Literal).Text = GetGlobalResourceObject("Versions", "ContentText_CallForPrice")
                        Continue For
                    End If
                    '' Creating the Currency Format for the Version's Price.
                    Dim numPrice As Single = CDbl(CType(e.Item.FindControl("litPrice_Rows"), Literal).Text)
                    CType(e.Item.FindControl("litResultedPrice_Rows"), Literal).Text =
                      CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice)

                Case "litResultedCalculatedTax_Rows"
                    Dim numPrice As Single = CDbl(CType(e.Item.FindControl("litCalculatedTax_Rows"), Literal).Text)
                    CType(e.Item.FindControl("litResultedCalculatedTax_Rows"), Literal).Text =
                      CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice)

                Case "litResultedIncTax_Rows"
                    Dim numPrice As Single = CDbl(CType(e.Item.FindControl("litIncTax_Rows"), Literal).Text)
                    CType(e.Item.FindControl("litResultedIncTax_Rows"), Literal).Text =
                      CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice)

                Case "litResultedExTax_Rows"
                    Dim numPrice As Single = CDbl(CType(e.Item.FindControl("litExTax_Rows"), Literal).Text)
                    CType(e.Item.FindControl("litResultedExTax_Rows"), Literal).Text =
                      CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice)

                Case "litResultedTaxRate_Rows"
                    CType(e.Item.FindControl("litResultedTaxRate_Rows"), Literal).Text =
                      CType(e.Item.FindControl("litTaxRate_Rows"), Literal).Text & "%"

                Case "updAddQty"
                    '' Adding the Quantity 1 To 10, for each row of the versions.

                Case "lblRRP_Rows"
                    '' Replacing the RRP zero value by empty string.
                    If CType(e.Item.FindControl("lblRRP_Rows"), Label).Text = "0" Then
                        CType(e.Item.FindControl("lblRRP_Rows"), Label).Text = "-"
                    End If

                Case "lblStock_Rows"
                    '' Replacing the Stock zero value by "-".
                    If CType(e.Item.FindControl("lblStock_Rows"), Label).Text = "0" Then
                        CType(e.Item.FindControl("lblStock_Rows"), Label).Text = "-"
                    End If

                Case "litRRP_Rows"
                    '' Creating the Currency Format for the Version's Price.
                    Dim numRRP As Single = CDbl(CType(e.Item.FindControl("litRRP_Rows"), Literal).Text)
                    CType(e.Item.FindControl("litRRP_Rows"), Literal).Text =
                      CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numRRP)

                Case "phdCustomizable"
                    If Session("BasketItemInfo") <> "" And Request.QueryString("strOptions") & "" <> "" Then
                        Dim arrBasketInfo() As String = Split(Session("BasketItemInfo"), ";")
                        Dim UC_AddToBasket As UserControls_General_AddToBasket
                        UC_AddToBasket = CType(e.Item.FindControl("UC_AddToBasketQty2"), UserControls_General_AddToBasket)
                        UC_AddToBasket.ItemEditVersionID = arrBasketInfo(1)
                        UC_AddToBasket.ItemEditQty = arrBasketInfo(2)
                    End If

                Case Else
            End Select
        Next
    End Sub

    ''' <summary>
    ''' Single version product display
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub fvwPrice_DataBound(ByVal sender As Object, ByVal e As System.EventArgs) Handles fvwPrice.DataBound
        Try
            ''Show the panel with the correct price display
            If KartSettingsManager.GetKartConfig("frontend.display.showtax") = "y" Then
                If KartSettingsManager.GetKartConfig("general.tax.pricesinctax") = "y" Then

                    ''Show inc/extax pricing
                    fvwPrice.FindControl("pnlExIncTax").Visible = True

                    ''Set the extax price (alongside inctax)
                    Dim numExTax1 As Single = CDbl(CType(fvwPrice.FindControl("litCalculatedTax_Rows"), Literal).Text)
                    CType(fvwPrice.FindControl("litResultedCalculatedTax_Rows"), Literal).Text =
                     CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numExTax1)

                    ''Set the inctax price
                    Dim numIncTax As Single = CDbl(CType(fvwPrice.FindControl("litIncTax_Rows"), Literal).Text)
                    CType(fvwPrice.FindControl("litResultedIncTax_Rows"), Literal).Text =
                     CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numIncTax)

                Else
                    ''Show extax pricing with tax rate
                    fvwPrice.FindControl("pnlExTaxTax").Visible = True

                    ''Set the extax price (alongside tax rate)
                    Dim numExTax2 As Single = CDbl(CType(fvwPrice.FindControl("litExTax_Rows"), Literal).Text)
                    CType(fvwPrice.FindControl("litResultedExTax_Rows"), Literal).Text =
                     CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numExTax2)

                    ''Set the tax rate %
                    CType(fvwPrice.FindControl("litResultedTaxRate_Rows"), Literal).Text =
                     CDbl(CType(fvwPrice.FindControl("litTaxRate_Rows"), Literal).Text) & " %"

                End If
            Else
                Dim objObjectConfigBLL As New ObjectConfigBLL
                If objObjectConfigBLL.GetValue("K:product.callforprice", _ProductID) <> 1 Then
                    ''Show single price
                    fvwPrice.FindControl("pnlPrice").Visible = True

                    ''Set the single price
                    Dim numPrice As Single = CDbl(CType(fvwPrice.FindControl("litPrice_Rows"), Literal).Text)
                    CType(fvwPrice.FindControl("litResultedPrice_Rows"), Literal).Text =
                     CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numPrice)
                End If

            End If
            ''Set the RRP
            Try
                Dim numRRP As Single = CDbl(CType(fvwPrice.FindControl("litRRP"), Literal).Text)
                CType(fvwPrice.FindControl("litRRP"), Literal).Text =
                 CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numRRP)
            Catch ex As Exception

            End Try

        Catch ex As Exception

        End Try
    End Sub

    '' Clears the saved selected images if any view was activated.
    Protected Sub mvwVersion_ActiveViewChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles mvwVersion.ActiveViewChanged
        'ddlVersionImages.Items.Clear()
    End Sub

    ''' <summary>
    ''' Handles add to basket button clicks for custom product versions
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub btnAddCustomVersions_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAddCustomVersion.Click
        Dim UC_CustomControl As KartrisClasses.CustomProductControl = phdCustomControl.FindControl("UC_CustomControl")
        If UC_CustomControl IsNot Nothing Then
            If UC_CustomControl.ComputeFromSelectedOptions() = "success" Then
                Dim objMiniBasket As Object = Page.Master.FindControl("UC_MiniBasket")
                Dim objBasket As Kartris.Basket = objMiniBasket.GetBasket
                objMiniBasket.AddCustomItemToBasket(litHiddenV_ID.Text, ddlCustomVersionQuantity.SelectedValue,
                                            UC_CustomControl.ParameterValues & "|||" & UC_CustomControl.ItemDescription & "|||" & UC_CustomControl.ItemPrice)
            End If
        End If
    End Sub

    ''' <summary>
    ''' Handles add to basket button clicks for dropdown versions
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub btnAddVersions3_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAddVersions3.Click
        If Not txtOutOfStockItems.Text.Contains("," & ddlName_DropDown.SelectedIndex & ",") Then
            Dim numQuantity As Single = UC_AddToBasketQty3.ItemsQuantity
            Dim numVersionID As Long = ddlName_DropDown.SelectedValue

            '' Unit size should be checked if the quantity control is "textbox" => qty entered by user
            Dim strUnitSize As String = UC_AddToBasketQty3.UnitSize

            '' Check for wrong quantity
            Dim numNoOfDecimalPlacesForUnit As Integer = IIf(strUnitSize.Contains("."), Mid(strUnitSize, strUnitSize.IndexOf(".") + 2).Length, 0)
            Dim numNoOfDecimalPlacesForQty As Integer = IIf(CStr(numQuantity).Contains(".") AndAlso CLng(Mid(CStr(numQuantity), CStr(numQuantity).IndexOf(".") + 2)) <> 0,
                                                            Mid(CStr(numQuantity), CStr(numQuantity).IndexOf(".") + 2).Length,
                                                            0)
            Dim numMod As Integer = CInt(numQuantity * Math.Pow(10, numNoOfDecimalPlacesForQty)) Mod CInt(strUnitSize * Math.Pow(10, numNoOfDecimalPlacesForUnit))
            If numMod <> 0.0F OrElse numNoOfDecimalPlacesForQty > numNoOfDecimalPlacesForUnit Then
                '' wrong quantity - quantity should be a multiplies of unit size
                AddWrongQuantity(GetGlobalResourceObject("Kartris", "ContentText_CorrectErrors"),
                        Replace(GetGlobalResourceObject("ObjectConfig", "ContentText_OrderMultiplesOfUnitsize"), "[unitsize]", strUnitSize))
            Else
                Dim sessionID As Long = Session("SessionID")
                AddItemsToBasket(numVersionID, numQuantity)

                'v2.9010 Autosave basket
                Try
                    BasketBLL.AutosaveBasket(DirectCast(Page, PageBaseClass).CurrentLoggedUser.ID)
                Catch ex As Exception
                    'User not logged in
                End Try
            End If
        Else
            phdOutOfStock3.Visible = True
            phdNotOutOfStock3.Visible = False
            'updVersionQty3.Update()
        End If
    End Sub

    ''' <summary>
    ''' Handles click of the 'add' button for options products
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub btnAdd_Options_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAdd_Options.Click
        Dim strNonSelectedOptions As String = UC_OptionsContainer.CheckForValidSelection()
        If strNonSelectedOptions Is Nothing Then

            '' Unit size should be checked if the quantity control is "textbox" => qty entered by user
            Dim strUnitSize As String = UC_AddToBasketQty4.UnitSize
            Dim numQuantity As Single = UC_AddToBasketQty4.ItemsQuantity

            '' Check for wrong quantity
            Dim numNoOfDecimalPlacesForUnit As Integer = IIf(strUnitSize.Contains("."), Mid(strUnitSize, strUnitSize.IndexOf(".") + 2).Length, 0)
            Dim numNoOfDecimalPlacesForQty As Integer = IIf(CStr(numQuantity).Contains(".") AndAlso CLng(Mid(CStr(numQuantity), CStr(numQuantity).IndexOf(".") + 2)) <> 0,
                                                            Mid(CStr(numQuantity), CStr(numQuantity).IndexOf(".") + 2).Length,
                                                            0)
            Dim numMod As Integer = CInt(numQuantity * Math.Pow(10, numNoOfDecimalPlacesForQty)) Mod CInt(strUnitSize * Math.Pow(10, numNoOfDecimalPlacesForUnit))
            If numMod <> 0.0F OrElse numNoOfDecimalPlacesForQty > numNoOfDecimalPlacesForUnit Then
                '' wrong quantity - quantity should be a multiplies of unit size
                AddWrongQuantity(GetGlobalResourceObject("Kartris", "ContentText_CorrectErrors"),
                        Replace(GetGlobalResourceObject("ObjectConfig", "ContentText_OrderMultiplesOfUnitsize"), "[unitsize]", strUnitSize))
            Else
                '' Reading the values of Options from the OptionsContainer in a muli-dimentional array
                Dim strOptionString As String = UC_OptionsContainer.GetSelectedOptions()

                If [String].IsNullOrEmpty(strOptionString) Then strOptionString = ""
                Dim objVersionsBLL As New VersionsBLL
                Dim numVersionID As Integer = objVersionsBLL.GetCombinationVersionID_s(_ProductID, strOptionString)
                If numVersionID <> 0 Then
                    lblVID_Options.Text = numVersionID
                Else
                    numVersionID = CInt(lblVID_Options.Text)
                End If

                AddItemsToBasket(numVersionID, numQuantity, strOptionString)

                'v2.9010 Autosave basket
                Try
                    BasketBLL.AutosaveBasket(DirectCast(Page, PageBaseClass).CurrentLoggedUser.ID)
                Catch ex As Exception
                    'User not logged in
                End Try
            End If

        Else
            Dim strOptionValidationMessage As String = Nothing

            Do While strNonSelectedOptions.Contains(",,")
                strNonSelectedOptions = Replace(strNonSelectedOptions, ",,", ",")
            Loop
            strNonSelectedOptions = Replace(strNonSelectedOptions, ",", "<br />" & vbCrLf)

            strOptionValidationMessage = "<div>" & GetGlobalResourceObject("Kartris", "ContentText_OptionsMissingSelection") &
              "</div><p><strong>" & strNonSelectedOptions & "</strong></p>"

            UC_PopupMessage.SetTitle = GetGlobalResourceObject("Kartris", "ContentText_Alert")
            UC_PopupMessage.SetTextMessage = strOptionValidationMessage
            UC_PopupMessage.SetWidthHeight(350, 200)
            UC_PopupMessage.ShowPopup()
            updOptionsContainer.Update()
        End If

    End Sub

    ''' <summary>
    ''' Handles options price change
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub UC_OptionsContainer_Event_OptionPriceChanged(ByVal pOptionPrice As Single) Handles UC_OptionsContainer.Event_OptionPriceChanged
        'Reading the values of Options from the OptionsContainer in a muli-dimentional array
        Dim strOptionsList As String = UC_OptionsContainer.GetSelectedOptions()
        CleanOptionString(strOptionsList)

        If UC_OptionsContainer.IsUsingCombinationPrices Then
            GetCombinationPrice()
        Else
            AddOptionsPrice(pOptionPrice)
        End If


        If Not [String].IsNullOrEmpty(strOptionsList) Then
            Dim objVersionsBLL As New VersionsBLL
            numVersionID = objVersionsBLL.GetCombinationVersionID_s(ProductID, strOptionsList)
            If numVersionID <> -1 Then CheckCombinationsImages(numVersionID)

        End If
    End Sub

    ''' <summary>
    ''' Check if there is a combination image for the selected item
    ''' </summary>
    Sub CheckCombinationsImages(ByVal numVersionID As Int64)

        Dim extensions As New List(Of String)
        extensions.Add("*.png")
        extensions.Add("*.jpg")
        extensions.Add("*.jpeg")

        'Let's find and create some objects we'll need to manipulate
        Dim updProduct As UpdatePanel
        Dim updImages As UpdatePanel
        Dim phdMainImages As PlaceHolder
        Dim phdCombinationImage As PlaceHolder
        Dim litTest As Literal

        updProduct = TryCast(Me.Parent.Parent.Parent.Parent.Parent, UpdatePanel)
        updImages = TryCast(updProduct.FindControl("updImages"), UpdatePanel)
        phdMainImages = TryCast(updProduct.FindControl("phdMainImages"), PlaceHolder)
        phdCombinationImage = TryCast(updProduct.FindControl("phdCombinationImage"), PlaceHolder)

        litTest = TryCast(updProduct.FindControl("litTest"), Literal)

        'phdCombinationImage.Controls.Clear()

        Try

            Dim versionImagesPath As String = HttpContext.Current.Server.MapPath("~") & "\Images\Products\" & ProductID & "\" & numVersionID
            If Directory.Exists(versionImagesPath) Then

                Dim fileCount As Integer
                For i As Integer = 0 To extensions.Count - 1
                    fileCount += Directory.GetFiles(versionImagesPath, extensions(i), SearchOption.AllDirectories).Length
                Next

                'updImages

                If fileCount > 0 Then
                    'Now let's set the image control to find a combination image,
                    'if it exists
                    Dim UC_ImageView As New ImageViewer


                    UC_ImageView = CType(updImages.FindControl("UC_CombinationImage"), ImageViewer)
                    UC_ImageView.ClearImages()
                    UC_ImageView.CreateImageViewer(IMAGE_TYPE.enum_VersionImage,
                                      numVersionID,
                                      KartSettingsManager.GetKartConfig("frontend.display.images.normal.height"),
                                      KartSettingsManager.GetKartConfig("frontend.display.images.normal.width"),
                                      "",
                                      _ProductID,
                                      ImageViewer.SmallImagesType.enum_ImageButton)

                    'Hide whole image and container if no image available, otherwise can end up
                    'with small square visible if there is a border and background set for the 
                    'image holder in CSS
                    If numVersionID > -1 Then
                        litTest.Text = numVersionID & " --- " & Now()
                        phdCombinationImage.Visible = True
                        phdMainImages.Visible = False
                    Else
                        phdCombinationImage.Visible = False
                        phdMainImages.Visible = True
                    End If

                    updImages.Update()
                Else
                    phdCombinationImage.Visible = False
                    phdMainImages.Visible = True
                End If
            Else
                phdCombinationImage.Visible = False
                phdMainImages.Visible = True
            End If
        Catch ex As Exception
            'if failure, probably no combination images, so let's hide anyway
            'phdCombinationImage.Visible = False
            'phdMainImages.Visible = True
        End Try

    End Sub

    ''' <summary>
    ''' Calculate options price change
    ''' </summary>
    ''' <remarks></remarks>
    Sub AddOptionsPrice(ByVal pOptionPrice As Single)
        Trace.Warn("UC_OptionsContainerEvent IN Versions")
        RaiseEvent Event_VersionPriceChanged(pOptionPrice)

        Dim litPriceTemp As New Literal
        litPriceTemp = CType(FindControl("litPriceHidden"), Literal)

        Dim numOldPrice As Single
        numOldPrice = CDbl(litPriceTemp.Text)
        Dim numNewPrice As Single
        numNewPrice = numOldPrice + CDbl(pOptionPrice)

        'Reading the values of Options from the OptionsContainer in a muli-dimentional array
        Dim strOptionString As String = UC_OptionsContainer.GetSelectedOptions()

        PricePreview(numNewPrice)
        CheckOptionStock(strOptionString)
        updPricePanel.Update()
        updOptions.Update()
    End Sub

    ''' <summary>
    ''' Get price of combination
    ''' </summary>
    ''' <remarks></remarks>
    Sub GetCombinationPrice()
        'Reading the values of Options from the OptionsContainer in a muli-dimentional array
        Dim strOptionsList As String = UC_OptionsContainer.GetSelectedOptions()
        CleanOptionString(strOptionsList)
        If Not [String].IsNullOrEmpty(strOptionsList) Then

            Dim objVersionsBLL As New VersionsBLL
            Dim numPrice As Single = objVersionsBLL.GetCombinationPrice(ProductID, strOptionsList)
            numVersionID = objVersionsBLL.GetCombinationVersionID_s(ProductID, strOptionsList)

            numPrice = CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), GetPriceWithGroupDiscount(numVersionID, numPrice))

            If numPrice <> Nothing Then
                phdNotOutOfStock4.Visible = True
                UC_AddToBasketQty4.Visible = True
                CheckOptionStock(strOptionsList)
                PricePreview(numPrice)
                phdNoValidCombinations.Visible = False 'hide the no-valid-combinations message
                updPricePanel.Update()
            Else
                HidePriceForInvalidCombination()
                phdNotOutOfStock4.Visible = False
                UC_AddToBasketQty4.Visible = False
            End If
        Else
            HidePriceForInvalidCombination()
            phdNotOutOfStock4.Visible = False
            UC_AddToBasketQty4.Visible = False
        End If
    End Sub

    ''' <summary>
    ''' Hide price if option selections don't match
    ''' any combination that is configured
    ''' </summary>
    ''' <remarks></remarks>
    Sub HidePriceForInvalidCombination()
        ''Hide prices if option selections don't
        'correspond to a particular combination that
        'exists
        phdNoValidCombinations.Visible = True  'show the no-valid-combinations message
        Dim objObjectConfigBLL As New ObjectConfigBLL
        If objObjectConfigBLL.GetValue("K:product.callforprice", _ProductID) <> 1 Then

            If ConfigurationManager.AppSettings("TaxRegime").ToLower = "us" Or ConfigurationManager.AppSettings("TaxRegime").ToLower = "simple" Then
                phdPrice.Visible = True
                litPriceView.Text = "-"
                Return
            End If

            Dim blnIncTax As Boolean = IIf(KartSettingsManager.GetKartConfig("general.tax.pricesinctax") = "y", True, False)
            Dim blnShowTax As Boolean = IIf(KartSettingsManager.GetKartConfig("frontend.display.showtax") = "y", True, False)

            If blnShowTax Then
                If blnIncTax Then
                    phdIncTax.Visible = False
                    phdExTax.Visible = False
                Else
                    phdExTax.Visible = False
                    phdTax.Visible = False
                End If
            Else
                phdPrice.Visible = False
            End If
            phdOutOfStock4.Visible = False
        End If

    End Sub

    ''' <summary>
    ''' Calculates and displays the price of an options product, based on selections
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub PricePreview(ByVal pPrice As Single)

        ''Handle 'call for prices' - determine whether to
        ''show the add to basket button or not
        Dim objObjectConfigBLL As New ObjectConfigBLL
        If objObjectConfigBLL.GetValue("K:product.callforprice", _ProductID) = 1 Then
            'PricePreview(0)
            phdIncTax.Visible = False
            phdExTax.Visible = False
            phdTax.Visible = False
            phdPrice.Visible = True
            litPriceView.Text = GetGlobalResourceObject("Versions", "ContentText_CallForPrice")
            'Hide add to basket button
            phdNotOutOfStock4.Visible = False
            UC_AddToBasketQty4.Visible = False
            Return
        End If

        If ConfigurationManager.AppSettings("TaxRegime").ToLower = "us" Or ConfigurationManager.AppSettings("TaxRegime").ToLower = "simple" Then
            phdPrice.Visible = True
            litPriceView.Text = CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), pPrice)
            Return
        End If

        Dim blnIncTax As Boolean = IIf(KartSettingsManager.GetKartConfig("general.tax.pricesinctax") = "y", True, False)
        Dim blnShowTax As Boolean = IIf(KartSettingsManager.GetKartConfig("frontend.display.showtax") = "y", True, False)

        Dim numTax As Single = litTaxRateHidden.Text
        Dim drwCurrencies As DataRow() = GetCurrenciesFromCache().Select("CUR_ID = " & Session("CUR_ID"))
        Dim numCalculatedTax As Single = CalculateTax(Decimal.Round(CDec(pPrice), drwCurrencies(0)("CUR_RoundNumbers")), numTax)
        drwCurrencies = Nothing

        If blnShowTax Then
            If blnIncTax Then
                phdIncTax.Visible = True
                litIncTax.Text = CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), pPrice)
                phdExTax.Visible = True
                litExTax.Text = CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), numCalculatedTax)
            Else
                phdExTax.Visible = True
                litExTax.Text = CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), pPrice)
                phdTax.Visible = True
                litTaxRate.Text = CStr(numTax) + " %"
            End If
        Else
            phdPrice.Visible = True
            litPriceView.Text = CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), pPrice)
        End If

    End Sub

    ''' <summary>
    ''' Version price changed event
    ''' </summary>
    ''' <remarks></remarks>
    Public Event Event_VersionPriceChanged(ByVal pVersionPrice As Single)

    ''' <summary>
    ''' Fires when items added to basket
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub AddItemsToBasket(ByVal pVersionID As Long, ByVal pQuantity As Single, Optional ByVal pOptions As String = Nothing)

        Dim objMiniBasket As Object = Page.Master.FindControl("UC_MiniBasket")
        Dim numVersionID As Long

        numVersionID = CLng(pVersionID)

        Dim arrBasketInfo() As String
        Dim numBasketItemID As Integer = 0
        Dim objObjectConfigBLL As New ObjectConfigBLL

        Dim strQuantityControl As String = LCase(objObjectConfigBLL.GetValue("K:product.addtobasketqty", ProductID))
        If Session("BasketItemInfo") & "" <> "" AndAlso (strQuantityControl = "dropdown" OrElse strQuantityControl = "textbox") Then
            arrBasketInfo = Split(Session("BasketItemInfo"), ";")
            numBasketItemID = arrBasketInfo(0)
        End If

        objMiniBasket.ShowCustomText(pVersionID, pQuantity, pOptions, numBasketItemID)

    End Sub

    ''' <summary>
    ''' Handles out-of-stock for options
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub CheckOptionStock(ByVal strOptionString As String)

        phdOutOfStock4.Visible = False
        phdNotOutOfStock4.Visible = True
        UC_AddToBasketQty4.Visible = True
        phdNoValidCombinations.Visible = False

        'Just clean up the options a bit
        If Not [String].IsNullOrEmpty(strOptionString) Then CleanOptionString(strOptionString)

        If [String].IsNullOrEmpty(strOptionString) OrElse strOptionString = "0" Then
            Return
        End If

        Dim objVersionsBLL As New VersionsBLL
        Dim numQty As Single = objVersionsBLL.GetOptionStockQty(_ProductID, strOptionString)

        'The version is not live or does not exist,
        'probably because invalid options selections
        If numQty = -9999.0F Then
            phdOutOfStock4.Visible = False
            phdNotOutOfStock4.Visible = False
            UC_AddToBasketQty4.Visible = False
            HidePriceForInvalidCombination()
            Return 'Stop right here
        End If

        'This handles showing 'out of stock' and hiding the 'add' button
        If objVersionsBLL.IsStockTrackingInBase(_ProductID) And numQty <= 0.0F And KartSettingsManager.GetKartConfig("frontend.orders.allowpurchaseoutofstock") <> "y" Then
            phdOutOfStock4.Visible = True
            UC_AddToBasketQty4.Visible = False
            phdNotOutOfStock4.Visible = False

            'Set the command argument for this item based
            'We use this for stock notifications popup
            numVersionID = objVersionsBLL.GetCombinationVersionID_s(_ProductID, strOptionString) 'This will find version ID for combination
            If numVersionID = 0 Then numVersionID = UC_AddToBasketQty4.VersionID 'Combination ID will be zero if options product, so just grab base Version ID
            btnNotifyMe4.CommandArgument = FormatStockNotificationDetails(numVersionID, _ProductID, "", Request.RawUrl.ToString.ToLower, Session("LANG"))
        End If
        updOptions.Update()
    End Sub

    ''' <summary>
    ''' Cleans up the string of options, removing double
    ''' commas where gaps occurred
    ''' </summary>
    ''' <remarks></remarks>
    Sub CleanOptionString(ByRef strOptionString As String)
        strOptionString = "," & strOptionString & ","

        'purge double commas
        Do While strOptionString.Contains(",,")
            strOptionString = Replace(strOptionString, ",,", ",")
        Loop

        Do While strOptionString.Contains(",0,")
            strOptionString = Replace(strOptionString, ",0,", ",")
        Loop

        If Not strOptionString Is Nothing Then
            If strOptionString.EndsWith(",") Then strOptionString = strOptionString.TrimEnd((","))
            If strOptionString.StartsWith(",") Then strOptionString = strOptionString.TrimStart((","))
        End If

        '' ------------------------------------------
        '' Code to remove the duplicate options.
        Dim arrOptions() As String = strOptionString.Split(",")
        For i As Integer = 0 To arrOptions.Length - 1
            For j As Integer = 0 To i
                If i <> j Then
                    If arrOptions(j) = arrOptions(i) Then arrOptions(j) = ""
                End If
            Next
        Next
        Dim sbdOptions As New StringBuilder("")
        For i As Integer = 0 To arrOptions.Length - 1
            If arrOptions(i) <> "" Then
                sbdOptions.Append(arrOptions(i))
                If i <> arrOptions.Length - 1 Then sbdOptions.Append(",")
            End If
        Next
        strOptionString = sbdOptions.ToString()
        '' ------------------------------------------
    End Sub

    ''' <summary>
    ''' Finds prices for customer group discount
    ''' </summary>
    ''' <remarks></remarks>
    Private Function GetPriceWithGroupDiscount(ByVal numVersionID As Long, ByVal numPrice As Double) As Double
        Dim objBasket As New Kartris.Basket
        Dim numCustomerID As Long = 0
        Dim numNewPrice, numCustomerGroupPrice As Double

        If HttpContext.Current.User.Identity.IsAuthenticated Then
            numCustomerID = CLng(DirectCast(Page, PageBaseClass).CurrentLoggedUser.ID)
        End If

        numCustomerGroupPrice = BasketBLL.GetCustomerGroupPriceForVersion(numCustomerID, numVersionID)
        If numCustomerGroupPrice > 0 Then
            numNewPrice = Math.Min(numCustomerGroupPrice, numPrice)
        Else
            numNewPrice = numPrice
        End If

        Return numNewPrice

    End Function

    ''' <summary>
    ''' Launches error popup if qty entered does not fit with
    ''' unitsize setting
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub AddWrongQuantity(ByVal strTitle As String, strMessage As String)
        Dim objMiniBasket As Object = Page.Master.FindControl("UC_MiniBasket")
        objMiniBasket.ShowPopupMini(strTitle, strMessage)
    End Sub

    ''' <summary>
    ''' Handles 'notify me' button clicks
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub StockNotificationHandler(ByVal sender As Object, ByVal e As CommandEventArgs)

        Dim btnNotifyMe As Button = DirectCast(sender, Button)

        Select Case btnNotifyMe.CommandName
            Case "StockNotificationDetails"

                'Break command argument string into four parts
                Dim aryStockNotificationDetails As Array = Split(btnNotifyMe.CommandArgument, "|||")
                UC_StockNotification.VersionID = CLng(aryStockNotificationDetails(0))
                UC_StockNotification.ProductID = CInt(aryStockNotificationDetails(1))
                UC_StockNotification.VersionName = Server.UrlDecode(aryStockNotificationDetails(2))
                UC_StockNotification.PageLink = Server.UrlDecode(aryStockNotificationDetails(3))
                UC_StockNotification.LanguageID = CByte(aryStockNotificationDetails(4))
                UC_StockNotification.ShowStockNotificationsPopup()

                Exit Select
        End Select
    End Sub

    ''' <summary>
    ''' Format the stock notification details into a triple-pipe
    ''' separated string so it can be passed as single command
    ''' argument
    ''' </summary>
    ''' <remarks></remarks>
    Public Function FormatStockNotificationDetails(ByVal numVersionID As Int64,
                                                    ByVal numProductID As Integer,
                                                    ByVal strVersionName As String,
                                                    ByVal strPageLink As String,
                                                    ByVal numLanguageID As Byte) As String
        Return (numVersionID.ToString & "|||" & numProductID.ToString & "|||" & Server.UrlEncode(strVersionName) & "|||" & Server.UrlEncode(strPageLink) & "|||" & numLanguageID.ToString)
    End Function

    ''' <summary>
    ''' This code tries to deal with occasional errors in the OptionsContainer.ascx.vb
    ''' file, where _ProductID ends up as -1. In that case, we raise an event called 
    ''' 'SomethingWentWrong()' there, and then act on it here.
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub UC_OptionsContainer_SomethingWentWrong() Handles UC_OptionsContainer.SomethingWentWrong
        UC_PopupMessage.SetTitle = GetGlobalResourceObject("Kartris", "ContentText_Alert")
        UC_PopupMessage.SetTextMessage = "Some kind of error occurred."
        UC_PopupMessage.SetWidthHeight(350, 200)
        UC_PopupMessage.ShowPopup()
        Response.Redirect(Request.RawUrl)
    End Sub

    ''' <summary>
    ''' Call for Price
    ''' </summary>
    ''' <remarks></remarks>
    Function ReturnCallForPrice(ByVal numP_ID As Int64) As Int16
        Dim objObjectConfigBLL As New ObjectConfigBLL
        Dim objValue As Object = objObjectConfigBLL.GetValue("K:product.callforprice", numP_ID)
        Return objValue
    End Function
End Class
