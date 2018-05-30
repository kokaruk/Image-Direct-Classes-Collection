<Serializable()>
Friend Class ImgDirDatesFortnightly
    Inherits ImgDirDates

    ' private field to indicate wether this customer to be released on odd weeks
    Private _fortnigtlyOddRelease As Nullable(Of Boolean)
    Private ReadOnly Property fortnigtlyOddRelease As Nullable(Of Boolean)
        Get
            If Not _fortnigtlyOddRelease.HasValue Then
                _fortnigtlyOddRelease = ImgdirDatesDB.isFortnOdd(customer)
            End If
            Return _fortnigtlyOddRelease
        End Get
    End Property

    Public Sub New(customer As ImgdirCustomer)
        MyBase.New(customer)
    End Sub

    Public Overrides Function makeCollectionPrefix() As String
        Dim myCulture As System.Globalization.CultureInfo = System.Globalization.CultureInfo.CurrentCulture
        Dim weekNumber = myCulture.Calendar.GetWeekOfYear(stopPeriodDateTime, System.Globalization.CalendarWeekRule.FirstFourDayWeek, 1)
        Return weekNumber.ToString()
    End Function

    Public Overrides Sub setDate() ' this method called from superclass constructor
        stopPeriodDateTime = parseTime(releaseTimes(0), endDateSearch())
        If Me.stopPeriodDateTime = Nothing Then Throw New Exception("No print Date Found")
        startPeriodDateTime = parseTime(releaseTimes(0), stopPeriodDateTime.AddDays(-14))
    End Sub

    Private Function endDateSearch() As Date

        Dim runDay As Date

        If (fortnigtlyOddRelease = isNowOddWeek()) Then
            Dim dayDiff As Integer = dateReceived.DayOfWeek - releaseTimes(0).DayOfWeek
            runDay = dateReceived.AddDays(-dayDiff)
            If DateTime.Compare(dateReceived, runDay) >= 0 Then
                runDay = runDay.AddDays(14)
            End If
        Else
            Dim dayDiff As Integer = dateReceived.DayOfWeek - releaseTimes(0).DayOfWeek
            runDay = dateReceived.AddDays(-dayDiff + 7)
        End If
        Return runDay
    End Function

    ' function to search for a runday, based on passed day parameter
    Private Function rundaySearch(searchingDate As Date)
        Do While Not (searchingDate.DayOfWeek = releaseTimes(0).DayOfWeek)
            searchingDate = searchingDate.AddDays(-1)
        Loop
        Return searchingDate
    End Function

    Private Function isNowOddWeek() As Boolean
        ' DateTime does not have a WeekNumber property. This however is available inside the CultureInfo calendar
        Dim myCulture As System.Globalization.CultureInfo = System.Globalization.CultureInfo.CurrentCulture
        Return ((myCulture.Calendar.GetWeekOfYear(dateReceived, System.Globalization.CalendarWeekRule.FirstFourDayWeek, 1) Mod 2) <> 0)
    End Function

End Class
