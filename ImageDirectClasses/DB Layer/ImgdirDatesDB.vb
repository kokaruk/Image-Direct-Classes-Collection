Option Explicit On
Option Strict On
Friend Module ImgdirDatesDB
    Friend Function getMonthlyDeliveryDays(imgDirCustomer As String) As MonthlyPrintWeekDays
        Dim sql As String = "SELECT deliveryday, printday " + _
                            "FROM releasetimesmonthly WHERE customer=@prinergy_customer " + _
                            "LIMIT 1; "
        Dim parameterNames() As String = {"@prinergy_customer"}
        Dim parameterVals() As String = {imgDirCustomer}
        ' Return New itemSize With  
        Dim rtTable As DataTable
        Try
            rtTable = ImgdirPostgresDataLayer.GetDataTable(sql, parameterNames, parameterVals)
        Catch ex As Exception
            Throw
        End Try
        If rtTable.Rows.Count = 1 Then
            Return New MonthlyPrintWeekDays With {.deliveryDay = CStr(rtTable.Rows(0).Item("deliveryday")), .printDay = CStr(rtTable.Rows(0).Item("printday"))}
        Else
            Throw New Exception(String.Format("No monthly realease times found for customer: {0}", imgDirCustomer))
        End If
    End Function

    ' determine if even or odd fortnighly releases
    Friend Function isFortnOdd(imgDirCusomer As ImgdirCustomer) As Boolean
        Dim sql As String = "SELECT fortnight_odd FROM releasetimes_fortnightly " + _
                            "WHERE custname=@prism_customer LIMIT 1;"
        Dim parameterNames() As String = {"@prism_customer"}
        Dim parameterVals() As String = {imgDirCusomer.prismCustName}
        Return CBool(ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals))
    End Function

End Module
