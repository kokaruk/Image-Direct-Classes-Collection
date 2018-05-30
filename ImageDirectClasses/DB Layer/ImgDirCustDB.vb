Option Explicit On
Option Strict On

Friend Module ImgDirCustDB

    Friend Function getPrinergyCustomer(prismCustomer As String) As String
        Dim sql As String = "SELECT prin_cust FROM customer WHERE rparent=@prism_customer LIMIT 1;"
        Dim parameterNames() As String = {"@prism_customer"}
        Dim parameterVals() As String = {prismCustomer}
        Return ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals)
    End Function

    Friend Function getPrismCustomer(ByVal prinergyCustomer As String) As String
        Dim sql As String = "SELECT rparent FROM customer WHERE prin_cust=@prinergy_customer LIMIT 1;"
        Dim parameterNames() As String = {"@prinergy_customer"}
        Dim parameterVals() As String = {prinergyCustomer}
        Return ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals)
    End Function

    Friend Function getReleaseTimes(imgCust As ImgdirCustomer) As ReleaseTimeInDB()
        Dim returnReleaseTimes As New ArrayList
        Dim sql As String = String.Format("SELECT rtime, rday FROM releasetimes_{0} WHERE custname=@prism_customer ORDER BY rday ASC;", imgCust.cycleType)
        Dim parameterNames() As String = {"@prism_customer"}
        Dim parameterVals() As String = {imgCust.prismCustName}
        Dim rtTable As DataTable
        Try
            rtTable = ImgdirPostgresDataLayer.GetDataTable(sql, parameterNames, parameterVals)
        Catch ex As Exception
            Throw
        End Try
        If rtTable.Rows.Count > 0 Then
            For Each row As DataRow In rtTable.Rows
                'rtime in database stored as period datatype
                'convert period type to dateTime type
                row.Item("rtime") = Convert.ToDateTime(row.Item("rtime").ToString())
                returnReleaseTimes.Add(New ReleaseTimeInDB With {.Time = CDate(row.Item("rtime")), .DayOfWeek = CInt(row.Item("rday"))})
            Next
        End If
        Return CType(returnReleaseTimes.ToArray(GetType(ReleaseTimeInDB)), ReleaseTimeInDB())
    End Function

    Friend Function getReleaseDescription(ByVal prismCustomer As String) As String
        Dim sql As String = "SELECT releases.description " + _
                            "FROM releases RIGHT JOIN customer " + _
                            "ON releases.code = customer.releases " + _
                            "WHERE rparent=@prism_customer LIMIT 1;"
        Dim parameterNames() As String = {"@prism_customer"}
        Dim parameterVals() As String = {prismCustomer}
        Return ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals)
    End Function

End Module
