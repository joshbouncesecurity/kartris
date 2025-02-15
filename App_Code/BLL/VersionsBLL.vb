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
Imports CkartrisEnumerations
Imports CkartrisBLL
Imports CkartrisFormatErrors
Imports System.Web.HttpContext
Imports kartrisVersionsData
Imports kartrisVersionsDataTableAdapters
Imports CkartrisDataManipulation
Imports KartSettingsManager
Public Class VersionsBLL

    Private _Adptr As VersionsTblAdptr = Nothing
    Protected ReadOnly Property Adptr() As VersionsTblAdptr
        Get
            _Adptr = New VersionsTblAdptr
            Return _Adptr
        End Get
    End Property

    Private _AdptrVersionOption As VersionOptionLinkTblAdptr = Nothing
    Protected ReadOnly Property AdptrVersionOption() As VersionOptionLinkTblAdptr
        Get
            _AdptrVersionOption = New VersionOptionLinkTblAdptr
            Return _AdptrVersionOption
        End Get
    End Property

    Private _AdptrQuantity As QuantityDiscountsTblAdptr = Nothing
    Protected ReadOnly Property AdptrQuantity() As QuantityDiscountsTblAdptr
        Get
            _AdptrQuantity = New QuantityDiscountsTblAdptr
            Return _AdptrQuantity
        End Get
    End Property

    Public Function GetByProduct(ByVal prodID As Integer, ByVal langID As Short, ByVal cgroup As Short) As Data.DataTable
        Return Adptr.GetByProductID(prodID, langID, cgroup)
    End Function

    Public Function GetProductOptions(ByVal _ProductID As Integer, ByVal _LangID As Short) As Data.DataTable
        Return Adptr.GetProductOptions(_ProductID, _LangID)
    End Function

    Public Function GetProductOptionValues(ByVal _ProductID As Integer, ByVal _LangID As Short, ByVal _OptionGroupID As Int32) As DataTable
        Return Adptr.GetOptionValues(_ProductID, _OptionGroupID, _LangID)
    End Function

    Public Function GetProductID_s(ByVal _VersionID As Long) As Integer
        Dim qAdptr As New VersionQTblAdptr
        Dim numProductID As Integer
        qAdptr.GetProductID_s(_VersionID, numProductID)
        Return numProductID
    End Function

    Public Function GetMinPriceByProductList(ByVal pProductList As String, ByVal pLanguageID As Short, ByVal pCGID As Short) As DataTable
        Return Adptr.GetMinPriceByProductList(pLanguageID, pProductList, pCGID)
    End Function

    Public Function GetOptionStockQty(ByVal ProductID As Integer, ByVal strOptionList As String) As Single
        Dim numQty As Single = -9999.0F
        Adptr.GetOptionStockQty(ProductID, strOptionList, numQty)
        Return numQty
    End Function

    Public Function GetVersionCustomization(ByVal numVersionID As Long) As DataTable
        Return Adptr.GetCustomization(numVersionID)
    End Function

    Public Function _GetWeightByVersionCode(ByVal strVersionCode As String) As Double
        Try
            Return CStr(Adptr._GetWeightByVersionCode(strVersionCode))
        Catch ex As Exception
            Return 0
        End Try
    End Function

    Public Function IsVersionCustomizable(ByVal numVersionID As Long) As Boolean
        Dim chrCustomizationType As Char = ""
        Try
            chrCustomizationType = CChar(GetVersionCustomization(numVersionID).Rows(0)("V_CustomizationType"))
            Return chrCustomizationType <> "n"
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Function GetBasicVersionByProduct(ByVal _ProductID As Integer) As DataTable
        Return Adptr.GetBasicVersionByProduct(_ProductID)
    End Function

    Public Function IsStockTrackingInBase(ByVal _ProductID As Integer) As Boolean
        Dim tblBaseVersion As DataTable = Adptr.GetBasicVersionByProduct(_ProductID)
        If tblBaseVersion.Rows.Count > 0 AndAlso
            tblBaseVersion.Rows(0)("V_QuantityWarnLevel") > 0.0F Then
            Return True
        End If
        Return False
    End Function

    Public Function _GetVersionByID(ByVal _VersionID As Long) As DataTable
        Return Adptr._GetByID(_VersionID)
    End Function

    Public Function _GetNameByVersionID(ByVal _VersionID As Integer, ByVal _LanguageID As Short) As String
        Dim objLanguageElementsBLL As New LanguageElementsBLL()
        Return objLanguageElementsBLL.GetElementValue(
          _LanguageID, LANG_ELEM_TABLE_TYPE.Versions, LANG_ELEM_FIELD_NAME.Name, _VersionID)
    End Function

    Public Function _GetStockLevel(ByVal numLanguageID As Byte) As DataTable
        Return Adptr._GetStockLevel(numLanguageID)
    End Function

    Public Function _SearchVersionByName(ByVal _Key As String, ByVal _LanguageID As Byte) As DataTable
        Dim tbl As New DataTable
        tbl = Adptr._SearchByName(_Key, _LanguageID)
        If tbl.Rows.Count = 0 Then
            tbl = Adptr._GetData(_LanguageID)
        End If
        Return tbl
    End Function

    Public Function _SearchVersionByCode(ByVal _Key As String) As DataTable
        Dim tbl As New DataTable
        Try
            tbl = Adptr._SearchByCode(_Key)
            If tbl.Rows.Count = 0 Then
                tbl = Adptr._SearchByCode("")
            End If
        Catch ex As Exception
            'hmmm
        End Try

        Return tbl
    End Function

    Public Function _SearchVersionByCodeExcludeBaseCombinations(ByVal _Key As String) As DataTable
        Dim tbl As New DataTable
        Try
            tbl = Adptr._SearchByCodeExcludeBaseCombinations(_Key)
            If tbl.Rows.Count = 0 Then
                tbl = Adptr._SearchByCodeExcludeBaseCombinations("")
            End If
        Catch ex As Exception
            'hmmm
        End Try

        Return tbl
    End Function

    Public Function _GetBasicVersionByProduct(ByVal _ProductID As Integer) As DataTable
        Return Adptr._GetBasicVersionByProduct(_ProductID)
    End Function

    Public Function _GetSingleVersionByProduct(ByVal _ProductID As Integer) As DataTable
        Return Adptr._GetSingleVersionByProduct(_ProductID)
    End Function

    Public Function _GetVersionOptionsByProductID(ByVal pProductID As Integer) As DataTable
        Return AdptrVersionOption._GetByProduct(pProductID)
    End Function

    Public Function _GetCombinationsByProductID(ByVal pProductID As Integer) As DataTable
        Return Adptr._GetCombinationsByProductID(pProductID)
    End Function

    Public Function _GetSuspendedByVersionID(ByVal pVersionID As Long, ByVal pLanguageID As Byte) As DataTable
        Return Adptr._GetSuspendedByVersionID(pVersionID, pLanguageID)
    End Function

    Public Function _GetSchema() As DataTable
        Return Adptr._GetByID(-1)
    End Function

    Private Function _GetRowsByProduct(ByVal pProductID As Integer) As DataTable
        Return Adptr._GetRowsByProductID(pProductID)
    End Function

    Public Function _GetByProduct(ByVal prodID As Integer, ByVal langID As Short) As Data.DataTable
        Return Adptr._GetByProductID(prodID, langID)
    End Function

    Public Function _IsCodeNumberExist(ByVal pCodeNumber As String, Optional ByVal pExcludedProductID As Integer = -1, Optional ByVal pExecludedVersionID As Long = -1) As Boolean
        Return Adptr._GetByCodeNumber(pCodeNumber, pExcludedProductID, pExecludedVersionID).Rows.Count > 0
    End Function

    Public Function _GetVersionIDByCodeNumber(ByVal pCodeNumber As String) As Int64
        Return Adptr._GetVersionIDByCodeNumber(pCodeNumber)
    End Function

    Public Function _GetNoOfVersionsByProductID(ByVal ProductID As Integer) As Integer
        Return CInt(Adptr._GetTotalByProductID(ProductID).Rows(0)("TotalVersions"))
    End Function

    Public Function _GetQuantityDiscountsByVersion(ByVal VersionID As Long) As DataTable
        Return AdptrQuantity._GetByVersion(VersionID)
    End Function

    Public Function _GetQuantityDiscountsByVersionIDList(ByVal strVersionIDList As String, ByVal LanguageID As Byte) As DataTable
        Return AdptrQuantity._GetByVersionIDList(strVersionIDList, LanguageID)
    End Function

    Public Function GetQuantityDiscountByProduct(ByVal ProductID As Integer, ByVal LanguageID As Byte) As DataTable
        Return AdptrQuantity.GetByProduct(ProductID, LanguageID)
    End Function

    Public Function _GetDownloadableFiles(ByVal numLanguageID As Byte) As DataTable
        Return Adptr._GetDownloadableFiles(numLanguageID)
    End Function

    Public Function _GetDownloadableLinks(ByVal numLanguageID As Byte) As DataTable
        Return Adptr._GetDownloadableLinks(numLanguageID)
    End Function
#Region "Markup Prices"

    Public Function _GetVersionsByCategoryList(ByVal numLanguageID As Byte, ByVal numFromPrice As Single, ByVal numToPrice As Single, ByVal strCategories As String) As DataTable
        Return Adptr._GetDetailsByCategoryList(numLanguageID, numFromPrice, numToPrice, strCategories)
    End Function

    Public Shared Function _MarkupPrices(ByVal strVersionsPricesList As String, strTargetField As String, ByRef strMsg As String) As Boolean
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdAddDiscount As SqlCommand = sqlConn.CreateCommand
            cmdAddDiscount.CommandText = "_spKartrisVersions_MarkupPrices"
            Dim savePoint As SqlTransaction = Nothing
            cmdAddDiscount.CommandType = CommandType.StoredProcedure
            Try
                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdAddDiscount.Transaction = savePoint
                cmdAddDiscount.Parameters.AddWithValue("@List", FixNullToDB(strVersionsPricesList))
                cmdAddDiscount.Parameters.AddWithValue("@Target", FixNullToDB(strTargetField))
                cmdAddDiscount.ExecuteNonQuery()
                savePoint.Commit()
                sqlConn.Close()
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function
#End Region

    Public Function _UpdateQuantityDiscount(ByVal tblQtyDiscount As DataTable, ByVal VersionID As Long, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdAddDiscount As SqlCommand = sqlConn.CreateCommand
            cmdAddDiscount.CommandText = "_spKartrisQuantityDiscounts_Add"
            Dim savePoint As SqlTransaction = Nothing
            cmdAddDiscount.CommandType = CommandType.StoredProcedure
            Try
                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdAddDiscount.Transaction = savePoint
                If Not _DeleteQtyDiscountByVersion(VersionID, sqlConn, savePoint) Then
                    Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
                End If

                For Each row As DataRow In tblQtyDiscount.Rows
                    cmdAddDiscount.Parameters.AddWithValue("@VersionID", VersionID)
                    cmdAddDiscount.Parameters.AddWithValue("@Quantity", CSng(row("QD_Quantity")))
                    cmdAddDiscount.Parameters.AddWithValue("@Price", CDbl(row("QD_Price")))

                    cmdAddDiscount.ExecuteNonQuery()
                    cmdAddDiscount.Parameters.Clear()
                Next

                savePoint.Commit()
                sqlConn.Close()
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False

    End Function

    Private Function _DeleteQtyDiscountByVersion(ByVal VersionID As Long, ByVal sqlConn As SqlConnection, ByVal savePoint As SqlTransaction) As Boolean
        Try
            Dim cmd As New SqlCommand("_spKartrisQuantityDiscounts_DeleteByVersion", sqlConn)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Transaction = savePoint

            cmd.Parameters.AddWithValue("@VersionID", VersionID)

            cmd.ExecuteNonQuery()
            Return True
            'End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
        End Try
        Return False

    End Function

    Public Function _GetCustomerGroupPricesForVersion(ByVal LanguageID As Integer, ByVal VersionID As Long) As DataTable
        Dim dtCustomerGroupPrices As New DataTable
        Dim daCustomerGroupPrices As New SqlDataAdapter

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdCustomerGroupPrices As SqlCommand = sqlConn.CreateCommand
            cmdCustomerGroupPrices.CommandText = "_spKartrisVersions_GetCustomerGroupPrices"
            cmdCustomerGroupPrices.CommandType = CommandType.StoredProcedure
            Try
                sqlConn.Open()
                cmdCustomerGroupPrices.Parameters.AddWithValue("@LanguageID", LanguageID)
                cmdCustomerGroupPrices.Parameters.AddWithValue("@VersionID", VersionID)
                daCustomerGroupPrices.SelectCommand = cmdCustomerGroupPrices
                cmdCustomerGroupPrices.ExecuteNonQuery()
                daCustomerGroupPrices.Fill(dtCustomerGroupPrices)
                sqlConn.Close()
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close()
            End Try
        End Using

        Return dtCustomerGroupPrices
    End Function

    Public Function _UpdateCustomerGroupPrice(ByVal CustomerGroupID As Integer, ByVal VersionID As Integer, ByVal Price As Double, ByVal CustomerGroupPriceID As Integer, ByRef strMsg As String) As Boolean
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Dim blnSuccess As Boolean = True

        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateCustomerGroupPrices As SqlCommand = sqlConn.CreateCommand
            cmdUpdateCustomerGroupPrices.CommandText = "_spKartrisVersions_UpdateCustomerGroupPrices"
            cmdUpdateCustomerGroupPrices.CommandType = CommandType.StoredProcedure
            Try
                cmdUpdateCustomerGroupPrices.Parameters.AddWithValue("@CustomerGroupID", CustomerGroupID)
                cmdUpdateCustomerGroupPrices.Parameters.AddWithValue("@VersionID", VersionID)
                cmdUpdateCustomerGroupPrices.Parameters.AddWithValue("@Price", Price)
                cmdUpdateCustomerGroupPrices.Parameters.AddWithValue("@CustomerGroupPriceID", CustomerGroupPriceID)
                sqlConn.Open()
                cmdUpdateCustomerGroupPrices.ExecuteNonQuery()
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
                blnSuccess = False
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close()
            End Try
        End Using

        Return blnSuccess
    End Function

#Region "   Combinations Work "
    Public Function GetCombinationVersionID_s(ByVal numProductID As Integer, ByVal strOptions As String) As Long
        Dim qAdptr As New VersionQTblAdptr
        Dim numVersionID As Long
        qAdptr.GetCombinationVersionID_s(numProductID, strOptions, numVersionID)
        Return numVersionID
    End Function

    Public Function _CreateNewCombinations(ByVal ptblNewData As DataTable, ByVal pProductID As Integer,
       ByVal pBasicVersionID As Long, ByRef strMsg As String) As Boolean

        '' From Kartris v3, a little different here. We want to keep the version IDs
        '' the same for combinations that already exist and data gets recovered, rather
        '' than copying it across to new versions. Therefore, the process is a little
        '' different now.
        '' 1. Accept two datatables of data - one for new versions, one for existing
        '' 2. Unsuspend existing versions that are needed by updating them with the
        ''    new data, and setting type back to 'c' (from 's')
        '' 3. Delete suspended combinations (any still left) using
        ''    [_spKartrisVersions_DeleteSuspendedVersions]
        ''    a. Delete the related records in the VersionLinkOptions
        ''    b. Delete the related records in the LanguageElements
        ''    c. Delete the records from Versions
        '' 4. Add new combinations with new IDs
        '' 5. Update the basic version To be of type 'b'

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim savePoint As SqlTransaction = Nothing
            Try

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()

                If Not _DeleteSuspendedCombinations(pProductID, sqlConn, savePoint) Then Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
                If Not _InsertNewCombinations(ptblNewData, pProductID, sqlConn, savePoint) Then Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))

                savePoint.Commit()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using

        Return False
    End Function

    Private Function _DeleteSuspendedCombinations(ByVal productID As Integer, ByVal sqlConn As SqlConnection, ByVal savePoint As SqlTransaction) As Boolean
        Try
            Dim cmd As New SqlCommand("_spKartrisVersions_DeleteSuspendedVersions", sqlConn, savePoint)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@P_ID", productID)
            cmd.ExecuteNonQuery()
            Return True
            'End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
        End Try
        Return False
    End Function

    Private Function _InsertNewCombinations(ByVal ptblCombinations As DataTable, ByVal pProductID As Integer, ByVal pSqlConn As SqlConnection, ByVal pSavePoint As SqlTransaction) As Boolean

        Try
            Dim tblLanguageElement As New DataTable
            tblLanguageElement.Columns.Add(New DataColumn("_LE_LanguageID", Type.GetType("System.Byte")))
            tblLanguageElement.Columns.Add(New DataColumn("_LE_FieldID", Type.GetType("System.Byte")))
            tblLanguageElement.Columns.Add(New DataColumn("_LE_Value", Type.GetType("System.String")))
            Dim tblLanguages As DataTable = GetLanguagesFromCache()
            Dim numNewVersionID As Long = 0
            For Each row As DataRow In ptblCombinations.Rows
                tblLanguageElement.Rows.Clear()
                For Each LangRow As DataRow In tblLanguages.Rows
                    tblLanguageElement.Rows.Add(CByte(LangRow("LANG_ID")), LANG_ELEM_FIELD_NAME.Name, CStr(row("V_Name")))
                    tblLanguageElement.Rows.Add(CByte(LangRow("LANG_ID")), LANG_ELEM_FIELD_NAME.Description, "")
                Next

                'Dim blnExists As Boolean = FixNullFromDB(row("IsExist"))
                'New Version
                If Not _AddNewVersionAsCombination(tblLanguageElement, CStr(row("V_CodeNumber")), CInt(row("V_ProductID")), CSng(row("V_Price")),
                 CByte(row("V_Tax")), FixNullFromDB(row("V_Tax2")), "", CSng(row("V_Weight")), CShort(row("V_Quantity")), CInt(row("V_QuantityWarnLevel")),
                 CSng(row("V_RRP")), CChar(row("V_Type")), pSqlConn, pSavePoint, numNewVersionID) Then

                    Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom") & " Check you have set a tax band for the base version.")
                End If

                CkartrisFormatErrors.LogError(CStr(row("V_CodeNumber")))

                '' Options
                Dim strIDList As String = CStr(row("ID_List"))
                If strIDList.EndsWith(",") Then strIDList = strIDList.TrimEnd(",")
                If strIDList.StartsWith(",") Then strIDList = strIDList.TrimStart(",")

                Dim cmdAddOptions As New SqlCommand("_spKartrisVersionOptionLink_AddOptionsList", pSqlConn, pSavePoint)
                cmdAddOptions.CommandType = CommandType.StoredProcedure
                cmdAddOptions.Parameters.AddWithValue("@VersionID", numNewVersionID)
                cmdAddOptions.Parameters.AddWithValue("@OptionList", strIDList)
                cmdAddOptions.ExecuteNonQuery()
                numNewVersionID = 0
            Next

            Return True
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
        End Try

        Return False
    End Function

    Public Function _AddNewVersionAsCombination(ByVal tblElements As DataTable, ByVal strCodeNumber As String, ByVal intProductID As Integer,
      ByVal decPrice As Decimal, ByVal intTaxID As Byte, intTaxID2 As Byte, strExtraTax As String, ByVal snglWeight As Single, ByVal sngStockQty As Single,
      ByVal sngWarnLevel As Single, ByVal decRRP As Decimal, ByVal chrType As Char,
      ByVal sqlConn As SqlConnection, ByVal savePoint As SqlTransaction, ByRef numNewVersionID As Long) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Dim cmdAddVersion As New SqlCommand("_spKartrisVersions_AddAsCombination", sqlConn)
        cmdAddVersion.CommandType = CommandType.StoredProcedure
        Try
            cmdAddVersion.Parameters.AddWithValue("@V_CodeNumber", FixNullToDB(strCodeNumber, "s"))
            cmdAddVersion.Parameters.AddWithValue("@V_ProductID", FixNullToDB(intProductID, "i"))
            cmdAddVersion.Parameters.AddWithValue("@V_Price", FixNullToDB(decPrice, "z"))
            cmdAddVersion.Parameters.AddWithValue("@V_Tax", FixNullToDB(intTaxID, "i"))
            cmdAddVersion.Parameters.AddWithValue("@V_Tax2", FixNullToDB(intTaxID2, "i"))
            cmdAddVersion.Parameters.AddWithValue("@V_TaxExtra", FixNullToDB(strExtraTax))
            cmdAddVersion.Parameters.AddWithValue("@V_Weight", snglWeight)
            cmdAddVersion.Parameters.AddWithValue("@V_Quantity", sngStockQty)
            cmdAddVersion.Parameters.AddWithValue("@V_QuantityWarnLevel", sngWarnLevel)
            cmdAddVersion.Parameters.AddWithValue("@V_RRP", FixNullToDB(decRRP, "z"))
            cmdAddVersion.Parameters.AddWithValue("@V_Type", FixNullToDB(chrType, "c"))
            cmdAddVersion.Parameters.AddWithValue("@V_NewID", 0).Direction = ParameterDirection.Output

            cmdAddVersion.Transaction = savePoint

            cmdAddVersion.ExecuteNonQuery()

            If cmdAddVersion.Parameters("@V_NewID").Value Is Nothing OrElse
              cmdAddVersion.Parameters("@V_NewID").Value Is DBNull.Value Then
                Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
            End If

            numNewVersionID = cmdAddVersion.Parameters("@V_NewID").Value
            If Not LanguageElementsBLL._AddLanguageElements(
              tblElements, LANG_ELEM_TABLE_TYPE.Versions, numNewVersionID, sqlConn, savePoint) Then
                Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
            End If

            Return True
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
        End Try
        'End Using
        Return False
    End Function

    Public Function _DeleteExistingCombinations(ByVal productID As Integer, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdSuspendVersions As SqlCommand = sqlConn.CreateCommand
            cmdSuspendVersions.CommandText = "_spKartrisVersions_SuspendProductVersions"
            Dim savePoint As SqlTransaction = Nothing
            cmdSuspendVersions.CommandType = CommandType.StoredProcedure
            Try
                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()

                cmdSuspendVersions.Transaction = savePoint
                cmdSuspendVersions.Parameters.AddWithValue("@P_ID", productID)
                cmdSuspendVersions.ExecuteNonQuery()
                If Not _DeleteSuspendedCombinations(productID, sqlConn, savePoint) Then Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))

                savePoint.Commit()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Public Function _UpdateCurrentCombinations(ByVal ptblCurrentCombinations As DataTable, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateCombination As SqlCommand = sqlConn.CreateCommand
            cmdUpdateCombination.CommandText = "_spKartrisVersions_UpdateCombinationVersion"
            Dim savePoint As SqlTransaction = Nothing
            cmdUpdateCombination.CommandType = CommandType.StoredProcedure

            Try
                cmdUpdateCombination.Parameters.Add("@ID", SqlDbType.BigInt)
                cmdUpdateCombination.Parameters.Add("@Name", SqlDbType.NVarChar, 50)
                cmdUpdateCombination.Parameters.Add("@CodeNumber", SqlDbType.NVarChar, 50)
                cmdUpdateCombination.Parameters.Add("@Price", SqlDbType.Real)
                cmdUpdateCombination.Parameters.Add("@StockQty", SqlDbType.Float)
                cmdUpdateCombination.Parameters.Add("@QtyWarnLevel", SqlDbType.Float)
                cmdUpdateCombination.Parameters.Add("@Live", SqlDbType.Bit)

                'See the explanation below in _UpdateVersion
                'for this parameter. It's a clever way of tagging
                'versions updated by data tool with datetime stamp
                'so we can process stock notifications for them
                cmdUpdateCombination.Parameters.AddWithValue("@V_BulkUpdateTimeStamp", "1900/1/1")
                sqlConn.Open()

                savePoint = sqlConn.BeginTransaction()
                cmdUpdateCombination.Transaction = savePoint
                For Each row As DataRow In ptblCurrentCombinations.Rows
                    cmdUpdateCombination.Parameters("@ID").Value = CLng(row("ID"))
                    cmdUpdateCombination.Parameters("@Name").Value = FixNullToDB(row("Name"), "s")
                    cmdUpdateCombination.Parameters("@CodeNumber").Value = FixNullToDB(row("CodeNumber"), "s")
                    cmdUpdateCombination.Parameters("@Price").Value = FixNullToDB(row("Price"), "g")
                    cmdUpdateCombination.Parameters("@StockQty").Value = row("StockQty")
                    cmdUpdateCombination.Parameters("@QtyWarnLevel").Value = row("QtyWarnLevel")
                    cmdUpdateCombination.Parameters("@Live").Value = FixNullToDB(row("Live"), "b")

                    cmdUpdateCombination.ExecuteNonQuery()
                Next
                savePoint.Commit()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")

                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Public Function GetCombinationPrice(ByVal numProductID As Integer, strOptions As String) As Single
        Dim qAdptr As New VersionQTblAdptr
        Dim numCombinationPrice As Single = 0.0F
        qAdptr.GetCombinationPrice_s(numProductID, strOptions, numCombinationPrice)
        Return FixNullFromDB(numCombinationPrice)
    End Function
#End Region

    Public Function _SetVersionAsBaseByProductID(
      ByVal intProductID As Long, ByVal sqlConn As SqlConnection,
      ByVal savePoint As SqlTransaction, ByRef strMsg As String) As Boolean

        Try
            Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
            Dim cmdUpdateVersion As New SqlCommand("_spKartrisVersions_SetBaseByProduct", sqlConn)
            cmdUpdateVersion.CommandType = CommandType.StoredProcedure
            cmdUpdateVersion.Parameters.AddWithValue("@ProductID", intProductID)
            cmdUpdateVersion.Transaction = savePoint
            cmdUpdateVersion.ExecuteNonQuery()
            Return True
            'End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
        End Try

        Return False
    End Function
    Public Function _AddNewVersionAsSingle(ByVal tblElements As DataTable, ByVal strCodeNumber As String, ByVal intProductID As Integer,
         ByVal intCustomerGrp As Short, ByVal sqlConn As SqlConnection, ByVal savePoint As SqlTransaction, ByRef strMsg As String) As Boolean

        Try
            Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
            Dim cmdAddVersion As New SqlCommand("_spKartrisVersions_AddAsSingle", sqlConn)

            cmdAddVersion.CommandType = CommandType.StoredProcedure

            cmdAddVersion.Parameters.AddWithValue("@V_CodeNumber", FixNullToDB(strCodeNumber, "s"))
            cmdAddVersion.Parameters.AddWithValue("@V_ProductID", FixNullToDB(intProductID, "i"))
            cmdAddVersion.Parameters.AddWithValue("@V_CustomerGroupID", FixNullToDB(intCustomerGrp, "i")) 'FixNullToDB(intCustomerGrp, "i"))
            cmdAddVersion.Parameters.AddWithValue("@V_NewID", 0).Direction = ParameterDirection.Output

            cmdAddVersion.Transaction = savePoint
            cmdAddVersion.ExecuteNonQuery()

            If cmdAddVersion.Parameters("@V_NewID").Value Is Nothing OrElse
              cmdAddVersion.Parameters("@V_NewID").Value Is DBNull.Value Then
                Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
            End If

            Dim intNewVersionID As Long = cmdAddVersion.Parameters("@V_NewID").Value
            If Not LanguageElementsBLL._AddLanguageElements(
              tblElements, LANG_ELEM_TABLE_TYPE.Versions, intNewVersionID, sqlConn, savePoint) Then
                Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
            End If

            strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
            'End Using
            Return True
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
        End Try

        Return False
    End Function

    Public Function _AddNewVersion(ByVal tblElements As DataTable, ByVal strCodeNumber As String, ByVal intProductID As Integer,
         ByVal decPrice As Decimal, ByVal intTaxID As Byte, ByVal intTaxID2 As Byte, ByVal strTaxExtra As String, ByVal snglWeight As Single,
         ByVal intDeliveryTime As Byte, ByVal sngStockQty As Single, ByVal sngWarnLevel As Single,
         ByVal blnLive As Boolean, ByVal strDownloadInfo As String, ByVal chrDownloadType As Char,
         ByVal decRRP As Decimal, ByVal chrType As Char,
         ByVal intCustomerGrp As Short, ByVal chrCustomizationType As Char, ByVal strCustomizationDesc As String,
         ByVal decCustomizationCost As Decimal, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdAddVersion As SqlCommand = sqlConn.CreateCommand
            cmdAddVersion.CommandText = "_spKartrisVersions_Add"
            Dim savePoint As SqlTransaction = Nothing
            cmdAddVersion.CommandType = CommandType.StoredProcedure
            Dim tblClone As DataTable = tblElements
            Try

                cmdAddVersion.Parameters.AddWithValue("@V_CodeNumber", FixNullToDB(strCodeNumber, "s"))
                cmdAddVersion.Parameters.AddWithValue("@V_ProductID", FixNullToDB(intProductID, "i"))
                cmdAddVersion.Parameters.AddWithValue("@V_Price", decPrice)
                cmdAddVersion.Parameters.AddWithValue("@V_Tax", FixNullToDB(intTaxID, "i"))
                cmdAddVersion.Parameters.AddWithValue("@V_Tax2", FixNullToDB(intTaxID2, "i"))
                cmdAddVersion.Parameters.AddWithValue("@V_TaxExtra", FixNullToDB(strTaxExtra, "s"))
                cmdAddVersion.Parameters.AddWithValue("@V_Weight", snglWeight)
                cmdAddVersion.Parameters.AddWithValue("@V_DeliveryTime", intDeliveryTime)
                cmdAddVersion.Parameters.AddWithValue("@V_Quantity", sngStockQty)
                cmdAddVersion.Parameters.AddWithValue("@V_QuantityWarnLevel", sngWarnLevel)
                cmdAddVersion.Parameters.AddWithValue("@V_Live", blnLive)
                cmdAddVersion.Parameters.AddWithValue("@V_DownLoadInfo", FixNullToDB(strDownloadInfo, "s"))
                cmdAddVersion.Parameters.AddWithValue("@V_DownloadType", FixNullToDB(chrDownloadType, "c"))
                cmdAddVersion.Parameters.AddWithValue("@V_RRP", decRRP)
                cmdAddVersion.Parameters.AddWithValue("@V_Type", FixNullToDB(chrType, "c"))
                cmdAddVersion.Parameters.AddWithValue("@V_CustomerGroupID", FixNullToDB(intCustomerGrp, "i")) 'FixNullToDB(intCustomerGrp, "i"))
                cmdAddVersion.Parameters.AddWithValue("@V_CustomizationType", chrCustomizationType)
                cmdAddVersion.Parameters.AddWithValue("@V_CustomizationDesc", FixNullToDB(strCustomizationDesc, "s"))
                cmdAddVersion.Parameters.AddWithValue("@V_CustomizationCost", FixNullToDB(decCustomizationCost, "z")) 'z=decimal, double is d
                cmdAddVersion.Parameters.AddWithValue("@V_NewID", 0).Direction = ParameterDirection.Output

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdAddVersion.Transaction = savePoint

                cmdAddVersion.ExecuteNonQuery()

                Dim intNewVersionID As Long = cmdAddVersion.Parameters("@V_NewID").Value
                If Not LanguageElementsBLL._AddLanguageElements(
                  tblClone, LANG_ELEM_TABLE_TYPE.Versions, intNewVersionID, sqlConn, savePoint) Then
                    Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
                End If

                If chrDownloadType = "u" AndAlso Not String.IsNullOrEmpty(strDownloadInfo) Then
                    Dim strUploadFolder As String = GetKartConfig("general.uploadfolder")
                    If File.Exists(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo)) Then
                        If File.Exists(Current.Server.MapPath(strUploadFolder & strDownloadInfo)) Then
                            File.Replace(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo),
                              Current.Server.MapPath(strUploadFolder & strDownloadInfo),
                              Current.Server.MapPath(strUploadFolder & "temp/backup_" & strDownloadInfo))
                        Else
                            File.Move(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo),
                                Current.Server.MapPath(strUploadFolder & strDownloadInfo))
                        End If
                    End If
                    If Not File.Exists(Current.Server.MapPath(strUploadFolder & strDownloadInfo)) Then
                        Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgFileUpload"))
                    End If
                End If

                savePoint.Commit()
                sqlConn.Close()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")

                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Public Function _UpdateVersion(ByVal tblElements As DataTable, ByVal lngVersionID As Long, ByVal strCodeNumber As String, ByVal intProductID As Integer,
         ByVal decPrice As Decimal, ByVal intTaxID As Byte, ByVal intTaxID2 As Byte, ByVal strTaxExtra As String, ByVal snglWeight As Single,
         ByVal intDeliveryTime As Byte, ByVal sngStockQty As Single, ByVal sngWarnLevel As Single,
         ByVal blnLive As Boolean, ByVal strDownloadInfo As String, ByVal chrDownloadType As Char,
         ByVal decRRP As Decimal, ByVal chrType As Char,
         ByVal intCustomerGrp As Short, ByVal chrCustomizationType As Char, ByVal strCustomizationDesc As String,
         ByVal decCustomizationCost As Decimal, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateVersion As SqlCommand = sqlConn.CreateCommand
            cmdUpdateVersion.CommandText = "_spKartrisVersions_Update"
            Dim savePoint As SqlTransaction = Nothing
            cmdUpdateVersion.CommandType = CommandType.StoredProcedure
            Dim tblClone As DataTable = tblElements

            Try
                cmdUpdateVersion.Parameters.AddWithValue("@V_ID", lngVersionID)
                cmdUpdateVersion.Parameters.AddWithValue("@V_CodeNumber", FixNullToDB(strCodeNumber, "s"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_ProductID", FixNullToDB(intProductID, "i"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_Price", decPrice)
                cmdUpdateVersion.Parameters.AddWithValue("@V_Tax", FixNullToDB(intTaxID, "i"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_Tax2", FixNullToDB(intTaxID2, "i"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_TaxExtra", FixNullToDB(strTaxExtra, "s"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_Weight", snglWeight)
                cmdUpdateVersion.Parameters.AddWithValue("@V_DeliveryTime", intDeliveryTime)
                cmdUpdateVersion.Parameters.AddWithValue("@V_Quantity", sngStockQty)
                cmdUpdateVersion.Parameters.AddWithValue("@V_QuantityWarnLevel", sngWarnLevel)
                cmdUpdateVersion.Parameters.AddWithValue("@V_Live", blnLive)
                cmdUpdateVersion.Parameters.AddWithValue("@V_DownLoadInfo", FixNullToDB(strDownloadInfo, "s"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_DownloadType", FixNullToDB(chrDownloadType, "c"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_RRP", decRRP)
                cmdUpdateVersion.Parameters.AddWithValue("@V_Type", FixNullToDB(chrType, "c"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_CustomerGroupID", FixNullToDB(intCustomerGrp, "i"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_CustomizationType", chrCustomizationType)
                cmdUpdateVersion.Parameters.AddWithValue("@V_CustomizationDesc", FixNullToDB(strCustomizationDesc, "s"))
                cmdUpdateVersion.Parameters.AddWithValue("@V_CustomizationCost", FixNullToDB(decCustomizationCost, "z")) 'z=decimal, d is for double

                'Ok, this optional parameter below is used for stock notification
                'functionality. If versions are updated through Kartris, we'll
                'check for and send stock notifications to users if the item is
                'now in stock. However, versions can also be updated by the data
                'tool. In that case, since this BLL function is not called, but
                'instead the _spKartrisVersions_Update sproc is called directly
                'we need a way to mark versions as having been bulk updated (i.e.
                'via data tool, not Kartris) so we can then provide a way in the
                'Kartris back end to manually trigger stock notification checks.
                'The way this works is that the sproc now has an optional parameter
                'for the V_BulkUpdateTimeStamp field. This defaults to NULL, which
                'is then coalesced within the the UPDATE query to be the date now.
                '(SQL doesn't allow us to default parameter to a function like
                'GetDate). So any version updated with the sproc directly will get
                'the current date in that field. However, we can pass a value in
                'to force this field to a different value. That's what we do below.
                'This means later we can query the version table and find any 
                'records with a V_BulkUpdateTimeStamp of a date which is not NULL
                'or 1900/1/1. Those ones were updated from the data tool and will
                'need stock notifications checked, which we can show in the task
                'list. When that routine is run, we can reset the V_BulkUpdateTimeStamp
                'field to 1900/1/1 or NULL. Good thing here is we also don't need to
                'update the data tool to pass extra parameters, because we have a
                'default value.
                cmdUpdateVersion.Parameters.AddWithValue("@V_BulkUpdateTimeStamp", "1900/1/1")
                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdUpdateVersion.Transaction = savePoint

                cmdUpdateVersion.ExecuteNonQuery()



                If chrType = "b" Then
                    Dim objObjectConfigBLL As New ObjectConfigBLL
                    If objObjectConfigBLL.GetValue("K:product.usecombinationprice", intProductID) <> "1" Then
                        If Not _UpdateCombinationsFromBasicInfo(intProductID, decPrice, intTaxID, intTaxID2, strTaxExtra, snglWeight, decRRP, sqlConn, savePoint) Then
                            Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
                        End If
                    End If
                End If

                If Not LanguageElementsBLL._UpdateLanguageElements(
                    tblClone, LANG_ELEM_TABLE_TYPE.Versions, lngVersionID, sqlConn, savePoint) Then
                    Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
                End If

                If chrDownloadType = "u" AndAlso Not String.IsNullOrEmpty(strDownloadInfo) Then
                    Dim strUploadFolder As String = GetKartConfig("general.uploadfolder")
                    If File.Exists(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo)) Then
                        If File.Exists(Current.Server.MapPath(strUploadFolder & strDownloadInfo)) Then
                            File.Replace(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo),
                              Current.Server.MapPath(strUploadFolder & strDownloadInfo),
                              Current.Server.MapPath(strUploadFolder & "temp/backup_" & strDownloadInfo))
                        Else
                            File.Move(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo),
                                Current.Server.MapPath(strUploadFolder & strDownloadInfo))
                        End If
                    End If
                    If Not File.Exists(Current.Server.MapPath(strUploadFolder & strDownloadInfo)) Then
                        Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgFileUpload"))
                    End If
                End If

                'Before we finish, note that a version has been
                'updated. It's possible that there might be some
                'people waiting on stock notifications. We can 
                'check first if this version is under stock
                'control, and is in stock. If so, we can run the
                'function to check for and send any stock
                'notifications.
                If GetKartConfig("general.stocknotification.enabled") = "y" And
                    sngStockQty > 0 And sngWarnLevel > 0 Then

                    'Stock notifications are enabled, this version is
                    'being stock tracked and has items in stock so we should
                    'run check to see if we should send stock notifications
                    StockNotificationsBLL._SearchSendStockNotifications(lngVersionID)

                End If

                savePoint.Commit()
                sqlConn.Close()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")

                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Public Function _UpdateVersionDownloadInfo(ByVal lngVersionID As Long, ByVal strDownloadInfo As String, ByVal chrDownloadType As Char, ByRef strMsg As String) As Boolean
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateVersion As SqlCommand = sqlConn.CreateCommand
            cmdUpdateVersion.CommandText = "_spKartrisVersions_UpdateDownloadInfo"
            Dim savePoint As SqlTransaction = Nothing
            cmdUpdateVersion.CommandType = CommandType.StoredProcedure

            Try
                cmdUpdateVersion.Parameters.AddWithValue("@V_ID", lngVersionID)
                cmdUpdateVersion.Parameters.AddWithValue("@V_DownLoadInfo", strDownloadInfo)

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdUpdateVersion.Transaction = savePoint

                cmdUpdateVersion.ExecuteNonQuery()

                If chrDownloadType = "u" Then
                    Dim strUploadFolder As String = GetKartConfig("general.uploadfolder")
                    If File.Exists(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo)) Then
                        If File.Exists(Current.Server.MapPath(strUploadFolder & strDownloadInfo)) Then
                            File.Replace(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo),
                              Current.Server.MapPath(strUploadFolder & strDownloadInfo),
                              Current.Server.MapPath(strUploadFolder & "temp/backup_" & strDownloadInfo))
                        Else
                            File.Move(Current.Server.MapPath(strUploadFolder & "temp/" & strDownloadInfo),
                                Current.Server.MapPath(strUploadFolder & strDownloadInfo))
                        End If

                        If Not File.Exists(Current.Server.MapPath(strUploadFolder & strDownloadInfo)) Then
                            Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgFileUpload"))
                        End If
                    End If
                End If

                savePoint.Commit()
                sqlConn.Close()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")

                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Public Function _UpdateVersionStockLevel(ByVal tblVersionsToUpdate As DataTable, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateVersionStock As SqlCommand = sqlConn.CreateCommand
            cmdUpdateVersionStock.CommandText = "_spKartrisVersions_UpdateStockLevel"
            Dim savePoint As SqlTransaction = Nothing
            cmdUpdateVersionStock.CommandType = CommandType.StoredProcedure
            Try
                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdUpdateVersionStock.Transaction = savePoint
                For Each row As DataRow In tblVersionsToUpdate.Rows
                    cmdUpdateVersionStock.Parameters.AddWithValue("@V_ID", row("VersionID"))
                    cmdUpdateVersionStock.Parameters.AddWithValue("@V_Quantity", row("StockQty"))
                    cmdUpdateVersionStock.Parameters.AddWithValue("@V_QuantityWarnLevel", row("WarnLevel"))

                    'See the explanation above in _UpdateVersion
                    'for this parameter. It's a clever way of tagging
                    'versions updated by data tool with datetime stamp
                    'so we can process stock notifications for them
                    cmdUpdateVersionStock.Parameters.AddWithValue("@V_BulkUpdateTimeStamp", "1900/1/1")

                    cmdUpdateVersionStock.ExecuteNonQuery()
                    cmdUpdateVersionStock.Parameters.Clear()

                    'Before we finish, note that a version has been
                    'updated. It's possible that there might be some
                    'people waiting on stock notifications. We can 
                    'check first if this version is under stock
                    'control, and is in stock. If so, we can run the
                    'function to check for and send any stock
                    'notifications.
                    If GetKartConfig("general.stocknotification.enabled") = "y" And
                        row("StockQty") > 0 And row("WarnLevel") > 0 Then

                        'Stock notifications are enabled, this version is
                        'being stock tracked and has items in stock so we should
                        'run check to see if we should send stock notifications
                        StockNotificationsBLL._SearchSendStockNotifications(row("VersionID"))
                    End If
                Next
                savePoint.Commit()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Public Function _UpdateVersionStockLevelByCode(ByVal tblVersionsToUpdate As DataTable, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateVersionStock As SqlCommand = sqlConn.CreateCommand
            cmdUpdateVersionStock.CommandText = "_spKartrisVersions_UpdateStockLevelByCode"
            Dim savePoint As SqlTransaction = Nothing
            cmdUpdateVersionStock.CommandType = CommandType.StoredProcedure
            cmdUpdateVersionStock.CommandTimeout = 2700
            Try
                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdUpdateVersionStock.Transaction = savePoint
                For Each row As DataRow In tblVersionsToUpdate.Rows
                    cmdUpdateVersionStock.Parameters.AddWithValue("@V_CodeNumber", row("VersionCode"))
                    cmdUpdateVersionStock.Parameters.AddWithValue("@V_Quantity", row("StockQty"))
                    cmdUpdateVersionStock.Parameters.AddWithValue("@V_QuantityWarnLevel", row("WarnLevel"))

                    'See the explanation above in _UpdateVersion
                    'for this parameter. It's a clever way of tagging
                    'versions updated by data tool with datetime stamp
                    'so we can process stock notifications for them
                    cmdUpdateVersionStock.Parameters.AddWithValue("@V_BulkUpdateTimeStamp", "1900/1/1")

                    cmdUpdateVersionStock.ExecuteNonQuery()
                    cmdUpdateVersionStock.Parameters.Clear()

                    'Before we finish, note that a version has been
                    'updated. It's possible that there might be some
                    'people waiting on stock notifications. We can 
                    'check first if this version is under stock
                    'control, and is in stock. If so, we can run the
                    'function to check for and send any stock
                    'notifications.
                    If GetKartConfig("general.stocknotification.enabled") = "y" And
                        row("StockQty") > 0 And row("WarnLevel") > 0 Then

                        'Stock notifications are enabled, this version is
                        'being stock tracked and has items in stock so we should
                        'run check to see if we should send stock notifications
                        StockNotificationsBLL._SearchSendStockNotifications(row("VersionID"))
                    End If
                Next
                savePoint.Commit()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Private Function _UpdateCombinationsFromBasicInfo(ByVal intProductID As Integer, ByVal snglPrice As Single, ByVal intTax As Byte,
              ByVal intTax2 As Byte, strTaxExtra As String, ByVal snglWeight As Single, ByVal snglRRP As Single,
              ByVal sqlConn As SqlConnection, ByVal savePoint As SqlTransaction) As Boolean
        Try
            Dim cmd As New SqlCommand("_spKartrisVersions_UpdateCombinationsFromBasicInfo", sqlConn)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Transaction = savePoint

            cmd.Parameters.AddWithValue("@ProductID", FixNullToDB(intProductID, "i"))
            cmd.Parameters.AddWithValue("@Price", FixNullToDB(snglPrice, "g"))
            cmd.Parameters.AddWithValue("@Tax", FixNullToDB(intTax, "i"))
            cmd.Parameters.AddWithValue("@Tax2", FixNullToDB(intTax2, "i"))
            cmd.Parameters.AddWithValue("@TaxExtra", FixNullToDB(strTaxExtra, "s"))
            cmd.Parameters.AddWithValue("@Weight", snglWeight)
            cmd.Parameters.AddWithValue("@RRP", snglRRP)

            cmd.ExecuteNonQuery()
            Return True
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
        End Try
        Return False
    End Function

    Public Function _DeleteVersion(ByVal VersionID As Long, ByRef strFiles As String, ByRef strMsg As String) As Boolean
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdDeleteVersion As SqlCommand = sqlConn.CreateCommand
            cmdDeleteVersion.CommandText = "_spKartrisVersions_Delete"
            Dim savePoint As SqlTransaction = Nothing
            cmdDeleteVersion.CommandType = CommandType.StoredProcedure

            Try
                cmdDeleteVersion.Parameters.AddWithValue("@V_ID", VersionID)
                cmdDeleteVersion.Parameters.Add("@DownloadFile", SqlDbType.NVarChar, 4000).Direction = ParameterDirection.Output
                cmdDeleteVersion.Parameters("@DownloadFile").Value = ""

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdDeleteVersion.Transaction = savePoint

                cmdDeleteVersion.ExecuteNonQuery()
                If Not cmdDeleteVersion.Parameters("@DownloadFile").Value Is DBNull.Value Then strFiles = cmdDeleteVersion.Parameters("@DownloadFile").Value
                savePoint.Commit()
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Public Function _DeleteProductVersions(ByVal ProductID As Integer, ByRef strFiles As String, ByVal strMsg As String) As Boolean
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdDeleteProductVersions As SqlCommand = sqlConn.CreateCommand
            cmdDeleteProductVersions.CommandText = "_spKartrisVersions_DeleteByProduct"
            Dim savePoint As SqlTransaction = Nothing
            cmdDeleteProductVersions.CommandType = CommandType.StoredProcedure

            Try
                cmdDeleteProductVersions.Parameters.AddWithValue("@P_ID", ProductID)
                cmdDeleteProductVersions.Parameters.Add("@DownloadFiles", SqlDbType.NVarChar, 4000).Direction = ParameterDirection.Output
                cmdDeleteProductVersions.Parameters("@DownloadFiles").Value = ""

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdDeleteProductVersions.Transaction = savePoint

                cmdDeleteProductVersions.ExecuteNonQuery()

                If Not OptionsBLL._DeleteProductOptionGroupByProductID(ProductID, sqlConn, savePoint) Then Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
                If Not OptionsBLL._DeleteProductOptionsByProductID(ProductID, sqlConn, savePoint) Then Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))

                savePoint.Commit()
                If Not cmdDeleteProductVersions.Parameters("@DownloadFiles").Value Is DBNull.Value Then strFiles = cmdDeleteProductVersions.Parameters("@DownloadFiles").Value
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close()
            End Try
        End Using
        Return False
    End Function

    Public Sub _ChangeSortValue(ByVal numVersionID As Long, ByVal numProductID As Integer, ByVal chrDirection As Char)
        Adptr._ChangeSortValue(numVersionID, numProductID, chrDirection)
    End Sub

    Public Function _UpdateCustomerGroupPriceList(strPriceList As String, ByRef numLineNumber As Integer, ByRef strMsg As String, ByRef blnUseVersionCode As Boolean) As Boolean

        'We want to parse the price list row here first, identify the parameters and 
        'then send these to sproc, rather than try to do the parsing there which seems
        'to be far more complicated.
        Dim numCG_ID As Integer = 0
        Dim numCGP_VersionID As Int64 = 0
        Dim strVersionCode As String = ""
        Dim numCGP_Price As Double = 0
        Dim numCounter As Int64 = 0

        Dim aryGroupPriceLines As String() = strPriceList.Split(New Char() {"#"c})
        Dim objVersionsBLL As New VersionsBLL

        For numCounter = 0 To aryGroupPriceLines.Length - 1
            Dim aryThisLine As String() = aryGroupPriceLines(numCounter).Split(New Char() {","c})
            Try
                numCG_ID = CInt(aryThisLine(1))
                If blnUseVersionCode Then
                    'We need to lookup the version ID from the code
                    numCGP_VersionID = objVersionsBLL._GetVersionIDByCodeNumber(aryThisLine(2))
                Else
                    numCGP_VersionID = CInt(aryThisLine(2))
                End If
                numCGP_Price = Convert.ToDouble(aryThisLine(3))

                'CG_Name','CG_ID','CGP_VersionID','CGP_Price'
                Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
                Using sqlConn As New SqlConnection(strConnString)
                    Dim cmd As SqlCommand = sqlConn.CreateCommand
                    cmd.CommandText = "_spKartrisVersions_UpdateCustomerGroupPriceList"
                    Dim savePoint As SqlTransaction = Nothing
                    cmd.CommandType = CommandType.StoredProcedure

                    Try
                        cmd.Parameters.AddWithValue("@CG_ID", numCG_ID)
                        cmd.Parameters.AddWithValue("@CGP_VersionID", numCGP_VersionID)
                        cmd.Parameters.AddWithValue("@CGP_Price", numCGP_Price)

                        sqlConn.Open()
                        savePoint = sqlConn.BeginTransaction()
                        cmd.Transaction = savePoint

                        cmd.ExecuteNonQuery()
                        savePoint.Commit()
                    Catch ex As Exception
                        If Not savePoint Is Nothing Then savePoint.Rollback()
                        ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
                    Finally
                        If sqlConn.State = ConnectionState.Open Then sqlConn.Close()
                    End Try
                End Using
            Catch ex As Exception
                'skip line, possibly first line which is strings, or errors
            End Try

        Next

        Return True
    End Function

    Public Function _UpdatePriceList(strPriceList As String, ByRef numLineNumber As Integer, ByRef strMsg As String) As Boolean
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmd As SqlCommand = sqlConn.CreateCommand
            cmd.CommandText = "_spKartrisVersions_UpdatePriceList"
            Dim savePoint As SqlTransaction = Nothing
            cmd.CommandType = CommandType.StoredProcedure

            Try
                cmd.Parameters.AddWithValue("@PriceList", strPriceList)
                cmd.Parameters.AddWithValue("@LineNumber", numLineNumber).Direction = ParameterDirection.Output

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmd.Transaction = savePoint

                cmd.ExecuteNonQuery()
                numLineNumber = cmd.Parameters("@LineNumber").Value
                savePoint.Commit()
                Return True
            Catch ex As Exception
                numLineNumber = cmd.Parameters("@LineNumber").Value
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close()
            End Try
        End Using
        Return False
    End Function
End Class

