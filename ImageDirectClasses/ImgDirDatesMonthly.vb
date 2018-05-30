<Serializable()>
Friend Class ImgDirDatesMonthly
    Inherits ImgDirDates

    Public Sub New(customer As ImgdirCustomer)
        MyBase.New(customer)
    End Sub

    Public Overrides Sub setDate()
        stopPeriodDateTime = parseTime(releaseTimes(0), endDateSearch())
        If Me.stopPeriodDateTime = Nothing Then Throw New Exception("No print Date Found")
        startPeriodDateTime = parseTime(releaseTimes(0), startDateSearch())
    End Sub

    Public Overrides Function makeCollectionPrefix() As String
        Return stopPeriodDateTime.Month.ToString()
    End Function

    Private Function endDateSearch() As Date
        ' find date for rundate in this month
        Dim runDay As Date = rundaySearch(New Date(dateReceived.Year, dateReceived.Month, DateTime.DaysInMonth(dateReceived.Year, dateReceived.Month)))
        ' lets compare two dates, if date received after release for this caledar month, the run should be rescheduled
        If DateTime.Compare(dateReceived, runDay) < 0 Then
            Return runDay
        Else
            runDay = rundaySearch(New Date(dateReceived.Year, dateReceived.AddMonths(1).Month, DateTime.DaysInMonth(dateReceived.Year, dateReceived.AddMonths(1).Month)))
            Return runDay
        End If
    End Function

    Private Function startDateSearch() As Date
        ' get end date,  get month before and loop back untill day of week
        ' equals to realease time day of week 
        Return rundaySearch(New Date(stopPeriodDateTime.Year, stopPeriodDateTime.AddMonths(-1).Month, DateTime.DaysInMonth(stopPeriodDateTime.Year, stopPeriodDateTime.AddMonths(-1).Month)))
    End Function


    ' function to search for a runday, based on passed day parameter
    Private Function rundaySearch(searchingDate As Date)
        Do While Not (searchingDate.DayOfWeek = releaseTimes(0).DayOfWeek)
            searchingDate = searchingDate.AddDays(-1)
        Loop
        Return searchingDate
    End Function

End Class
