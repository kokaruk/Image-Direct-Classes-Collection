<Serializable()>
Friend Class ImgDirDatesQuarterly
    Inherits ImgDirDates

    Private Const MONTHS_IN_QUARETR As Integer = 3
    Private _quarterMonth As Integer


    Public Sub New(customer As ImgdirCustomer)
        MyBase.New(customer)
    End Sub

    Public Overrides Function makeCollectionPrefix() As String
        Return _quarterMonth.ToString()
    End Function

    Public Overrides Sub setDate() ' this method called from superclass constructor
        _quarterMonth = Math.Floor((dateReceived.Month + 2) / MONTHS_IN_QUARETR) * MONTHS_IN_QUARETR + 1 ' searches for quarter end month
        stopPeriodDateTime = parseTime(releaseTimes(0), endDateSearch())
        If Me.stopPeriodDateTime = Nothing Then Throw New Exception("No print Date Found")
        startPeriodDateTime = parseTime(releaseTimes(0), startDateSearch())
        ' confirm that order arrived within start / end timeframe, 
        ' If dateReceived before the start date, need to reassign end date to start date, and re-run start date search
        If dateReceived < startPeriodDateTime Then
            stopPeriodDateTime = startPeriodDateTime
            startPeriodDateTime = parseTime(releaseTimes(0), startDateSearch())
            _quarterMonth = stopPeriodDateTime.Month
        End If
    End Sub

    Private Function endDateSearch() As Date
        Dim endDate As Date
        ' check if not the end of the year 
        If _quarterMonth <= 12 Then
            endDate = rundaySearch(New Date(dateReceived.Year, _quarterMonth, DateTime.DaysInMonth(dateReceived.Year, _quarterMonth)))
        ElseIf _quarterMonth = 13 Then ' new start of year
            _quarterMonth -= 12
            endDate = rundaySearch(New Date(dateReceived.Year + 1, _quarterMonth, DateTime.DaysInMonth(dateReceived.Year + 1, _quarterMonth)))
        End If
        Return endDate
    End Function

    Private Function startDateSearch() As Date

        Return rundaySearch(New Date(stopPeriodDateTime.AddMonths(-MONTHS_IN_QUARETR).Year, _
                                    stopPeriodDateTime.AddMonths(-MONTHS_IN_QUARETR).Month, _
                                    DateTime.DaysInMonth(stopPeriodDateTime.AddMonths(-MONTHS_IN_QUARETR).Year, stopPeriodDateTime.AddMonths(-MONTHS_IN_QUARETR).Month)))
    End Function

    ' function to search for a runday, based on passed day parameter
    Private Function rundaySearch(searchingDate As Date)
        Do While Not (searchingDate.DayOfWeek = releaseTimes(0).DayOfWeek)
            searchingDate = searchingDate.AddDays(-1)
        Loop
        Return searchingDate
    End Function


End Class
