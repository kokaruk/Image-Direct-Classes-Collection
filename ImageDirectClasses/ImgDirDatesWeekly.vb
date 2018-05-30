<Serializable()>
Friend Class ImgDirDatesWeekly
    Inherits ImgDirDates

    Private preCompReleaseModifier As Integer

    Private _period As Integer = Nothing
    Public Property period As Integer
        Get
            If _period = 0 Then
                _period = periodSearch()
            End If
            Return _period
        End Get
        Private Set(value As Integer)
            _period = value
        End Set
    End Property

    Public Sub New(ByVal customer As ImgdirCustomer, ByVal preCompReleaseModifier As Integer)
        MyBase.New(customer)
        Me.preCompReleaseModifier = preCompReleaseModifier
    End Sub

    Public Sub New(customer As ImgdirCustomer)
        MyClass.New(customer, 0)
    End Sub


    Public Overrides Sub setDate()
        Dim period As Integer = Me.period - 1

        Dim startOfPeriodReleaseTimeInDB As ReleaseTimeInDB

        If period = 0 Then
            startOfPeriodReleaseTimeInDB = releaseTimes(releaseTimes.GetUpperBound(0))
        Else
            startOfPeriodReleaseTimeInDB = releaseTimes(period - 1)
        End If
        startPeriodDateTime = startPeriodDateSearch(startOfPeriodReleaseTimeInDB)
        Dim stopOfPeriodReleaseTimeInDB As ReleaseTimeInDB = releaseTimes(Me.periodSearch() - 1)
        Me.stopPeriodDateTime = Me.stopPeriodDateSearch(stopOfPeriodReleaseTimeInDB)
    End Sub

    Private Function startPeriodDateSearch(ByVal startOfPeriodReleaseTime As ReleaseTimeInDB) As Date
        Dim triggerTime As Date = dateReceived
        Do Until (triggerTime.DayOfWeek = startOfPeriodReleaseTime.DayOfWeek)
            triggerTime = triggerTime.AddDays(-1)
        Loop
        Return parseTime(startOfPeriodReleaseTime, triggerTime)
    End Function
    Private Function stopPeriodDateSearch(ByVal stopOfPeriodReleaseTime As ReleaseTimeInDB) As Date
        Dim triggerTime As Date = Me.dateReceived
        Do
            triggerTime = triggerTime.AddDays(1)
        Loop Until (triggerTime.DayOfWeek = stopOfPeriodReleaseTime.DayOfWeek)
        Return parseTime(stopOfPeriodReleaseTime, triggerTime)
    End Function

    Public Overrides Function makeCollectionPrefix() As String
        Return String.Format("{0}-{1}", weekSearch.ToString(), period.ToString())
    End Function

    '
    '   Week Number Search
    '
    Private Function weekSearch() As Integer

        ' DateTime does not have a WeekNumber property. This however is available inside the CultureInfo calendar
        ' Week begins on the last day of cycles 
        Dim startDayOfWeek As Integer = releaseTimes(releaseTimes.GetUpperBound(0)).DayOfWeek + 1

        'reset startDayOfWeek, because in Au culture Sun is the first day of week. 
        If startDayOfWeek = 7 Then startDayOfWeek = 0

        Dim myCulture As System.Globalization.CultureInfo = System.Globalization.CultureInfo.CurrentCulture
        Dim WeekOfYear As Integer = myCulture.Calendar.GetWeekOfYear(dateReceived, System.Globalization.CalendarWeekRule.FirstFourDayWeek, startDayOfWeek)

        ' --- Find if creating before or after weeks cutoff time '
        ' --- Check if production week number needs to be decreased because of  off time --- ---
        Dim startGap As Date = releaseTimes(releaseTimes.GetUpperBound(0)).Time.AddMinutes(preCompReleaseModifier)

        If ((dateReceived.DayOfWeek = releaseTimes(releaseTimes.GetUpperBound(0)).DayOfWeek AndAlso _
        dateReceived.TimeOfDay > startGap.TimeOfDay)) Then WeekOfYear += 1
        Return WeekOfYear
    End Function

    Private Function periodSearch() As Integer
        ' test if order arrived in first period    
        If ((dateReceived.DayOfWeek > releaseTimes(releaseTimes.GetUpperBound(0)).DayOfWeek OrElse dateReceived.DayOfWeek < releaseTimes(0).DayOfWeek) OrElse _
            (dateReceived.DayOfWeek = releaseTimes(releaseTimes.GetUpperBound(0)).DayOfWeek AndAlso dateReceived.TimeOfDay > releaseTimes(releaseTimes.GetUpperBound(0)).Time.AddMinutes(preCompReleaseModifier).TimeOfDay) OrElse _
            (dateReceived.DayOfWeek = releaseTimes(0).DayOfWeek AndAlso dateReceived.TimeOfDay <= releaseTimes(0).Time.AddMinutes(preCompReleaseModifier).TimeOfDay)) Then

            Return 1
        Else
            ' if xml drop in other than first periods, search for period
            Dim ii As Integer = 0
            Do While (ii < releaseTimes.GetUpperBound(0))
                If ((dateReceived.DayOfWeek > releaseTimes(ii).DayOfWeek AndAlso dateReceived.DayOfWeek < releaseTimes(ii + 1).DayOfWeek) OrElse _
                    (dateReceived.DayOfWeek = releaseTimes(ii).DayOfWeek AndAlso dateReceived.TimeOfDay > releaseTimes(ii).Time.AddMinutes(preCompReleaseModifier).TimeOfDay) OrElse _
                   (dateReceived.DayOfWeek = releaseTimes(ii + 1).DayOfWeek AndAlso dateReceived.TimeOfDay <= releaseTimes(ii + 1).Time.AddMinutes(preCompReleaseModifier).TimeOfDay)) Then
                    Return ii + 2
                End If
                ii += 1
            Loop
        End If
        'if nothing found asume this is period 1
        Return 1
    End Function


End Class
