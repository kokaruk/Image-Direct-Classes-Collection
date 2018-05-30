Option Explicit On
Option Strict On

Friend Module ImgDirOrderItemDB
    Friend Function getUOM(ByVal itemCode As String) As Integer
        Dim sql As String = "SELECT productgroup.uom " + _
                            "FROM productgroup INNER JOIN productgroupmembers " + _
                            "ON productgroup.id=productgroupmembers.groupid " + _
                            "WHERE productgroupmembers.stockcode=@item_code LIMIT 1; "
        Dim parameterNames() As String = {"@item_code"}
        Dim parameterVals() As String = {itemCode}
        Return CInt(ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals))
    End Function

    Friend Function getPress(ByVal itemCode As String) As String
        Dim sql As String = "SELECT process FROM productview " + _
                            "WHERE stockcode=@item_code LIMIT 1; "
        Dim parameterNames() As String = {"@item_code"}
        Dim parameterVals() As String = {itemCode}
        Return ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals)
    End Function

    Friend Function getSize(ByVal itemCode As String) As ItemSize
        Dim sql As String = "SELECT imposition.sizex As ""SizeX"",  imposition.sizey As ""SizeY"" FROM imposition " + _
                            "LEFT JOIN productgroup " + _
                            "ON imposition.id = productgroup.impid " + _
                            "LEFT JOIN productgroupmembers " + _
                            "ON productgroup.id = productgroupmembers.groupid " + _
                            "WHERE stockcode = @item_code " + _
                            "LIMIT 1; "
        Dim parameterNames() As String = {"@item_code"}
        Dim parameterVals() As String = {itemCode}

        ' Return New itemSize With  
        Dim rtTable As DataTable
        Try
            rtTable = ImgdirPostgresDataLayer.GetDataTable(sql, parameterNames, parameterVals)
        Catch ex As Exception
            Throw
        End Try
        If rtTable.Rows.Count = 1 Then
            Return New ItemSize With {.width = New Size(CInt(rtTable.Rows(0).Item("SizeX"))), .height = New Size(CInt(rtTable.Rows(0).Item("SizeY")))}
        Else
            Throw New Exception(String.Format("No Sizes found for the item: {0}", itemCode))
        End If
    End Function

    Friend Function getProductGroupId(ByVal itemCode As String) As String
        Dim sql As String = "SELECT groupid " + _
                            "FROM productgroupmembers " + _
                            "WHERE stockcode = @item_code; "
        Dim parameterNames() As String = {"@item_code"}
        Dim parameterVals() As String = {itemCode}
        Return ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals)
    End Function

    Friend Function createNewItemRecordInDb(ByVal itemName As String, ByVal prismCustomer As String) As String
        Dim sql As String = "INSERT INTO asset(name, type, status, tlc, custid ) " + _
                            "VALUES (@product_name, 1, 0,  current_timestamp, " + _
                            "(SELECT id FROM customer WHERE rparent=@prism_customer)); " + _
                            "SELECT currval('aid'::regclass) as asset_id; "
        Dim parameterNames() As String = {"@product_name", "@prism_customer"}
        Dim parameterVals() As String = {itemName, prismCustomer}
        Dim assetId As String = ImgdirPostgresDataLayer.insertSelectTransaction(sql, parameterNames, parameterVals)
        insertAauditRecordInDb(assetId, "CREATE: " + itemName)
        Return assetId
    End Function

    Friend Function updateItemRecordInDb(ByVal assetId As String, ByVal updateInfo As String) As Integer
        insertAauditRecordInDb(assetId, updateInfo)
        Dim sql As String = "UPDATE asset " _
                          + "SET tlc=(SELECT DISTINCT tc " _
                          + "FROM aaudit WHERE id=@asset_id " _
                          + "AND seq = (SELECT DISTINCT MAX(seq) " _
                          + "FROM aaudit WHERE id=@asset_id ) LIMIT 1) " _
                          + If(updateInfo.Contains("DESTROY/CANCEL"), ", status = 3", String.Empty) _
                          + "WHERE id=@asset_id; "
        Dim parameterNames() As String = {"@asset_id"}
        Dim parameterVals() As String = {assetId}
        Return ImgdirPostgresDataLayer.ExecuteNonQuery(sql, parameterNames, parameterVals)
    End Function

    Private Function insertAauditRecordInDb(ByVal assetId As String, ByVal updateInfo As String) As Integer
        Dim sql As String = "INSERT INTO aaudit " + _
                            "VALUES (@asset_id, coalesce((select max(seq)+1 " + _
                            "FROM aaudit where id=@asset_id),1), 1, @update_info); "
        Dim parameterNames() As String = {"@asset_id", "@update_info"}
        Dim parameterVals() As String = {assetId, updateInfo}
        Return ImgdirPostgresDataLayer.updateTransaction(sql, parameterNames, parameterVals)
    End Function

End Module
