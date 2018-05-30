Option Explicit On
Option Strict On

Friend Module ImgdirOrderCollectionDB
    Friend Function dbOrdersCount(ByVal prinergyCustomer As String, _
                                                   ByVal startPeriodDateTime As Date, _
                                                   ByVal stopPeriodDateTime As Date) As Integer
        Dim sql As String = "SELECT count(distinct asset.id) as ""Counter""  " + _
                            "FROM asset LEFT JOIN customer " + _
                            "ON asset.custid = customer.id " + _
                            "WHERE customer.prin_cust = @prinergy_customer " + _
                            "AND asset.tc > @start_period_datetime " + _
                            "AND asset.tc < @stop_period_datetime; "
        Dim parameterNames() As String = {"@prinergy_customer", "@start_period_datetime", "@stop_period_datetime"}
        Dim parameterVals() As String = {prinergyCustomer, startPeriodDateTime.ToString("s"), stopPeriodDateTime.ToString("s")}
        Return CInt(ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals))
    End Function

    Friend Function getImpositionGroupTable(ByVal groupID As String) As DataTable
        Dim sql As String = "SELECT imposition.method, " + _
                            "imposition.sqty, " + _
                            "imposition.name As ""templatePath"", " + _
                            "productgroup.type, " + _
                            "productgroup.name, " + _
                            "productgroup.emptyspace, " + _
                            "productgroup.UOM, " + _
                            "process.process, " + _
                            "COALESCE(process.dest,'0') As jdfDest " + _
                            "FROM productgroup " + _
                            "RIGHT JOIN imposition " + _
                            "ON productgroup.impid = imposition.id " + _
                            "INNER JOIN process " + _
                            "ON CAST(COALESCE(imposition.process,'0') AS INTEGER )  = process.id " + _
                            "WHERE productgroup.id = @group_id; "
        Dim parameterNames() As String = {"@group_id"}
        Dim parameterVals() As String = {groupID}
        Return ImgdirPostgresDataLayer.GetDataTable(sql, parameterNames, parameterVals)
    End Function

    Friend Function getEmailDestination(ByVal groupId As String) As String
        Dim sql As String = "SELECT array_to_string( array_agg(email), '; ') AS ""email"" " + _
                            "FROM email_groups " + _
                            "WHERE id = any( regexp_split_to_array( (SELECT email from productgroup where id = CAST( @group_id AS INTEGER) ), ',')::int[]); "
        Dim parameterNames() As String = {"@group_id"}
        Dim parameterVals() As String = {groupId}
        Return ImgdirPostgresDataLayer.SelectScalar(sql, parameterNames, parameterVals)
    End Function

End Module
