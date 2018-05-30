<Serializable()>
Friend MustInherit Class ImgDirDates

    Protected Friend Property customer As ImgdirCustomer
    
    Private _dateReceived As Date
    Protected Friend Property dateReceived As Date
        Get
            Return _dateReceived
        End Get
        Private Set(value As Date)
            _dateReceived = value
        End Set
    End Property

    Private _startPeriodDateTime As Date
    Public Property startPeriodDateTime As Date
        Get
            Return _startPeriodDateTime
        End Get
        Set(value As Date)
            _startPeriodDateTime = value
        End Set
    End Property
    Private _stopPeriodDateTime As Date
    Public Property stopPeriodDateTime As Date
        Get
            Return _stopPeriodDateTime
        End Get
        Set(value As Date)
            _stopPeriodDateTime = value
        End Set
    End Property
    

    Private _releaseTimes() As ReleaseTimeInDB = New ReleaseTimeInDB() {}
    Public Property releaseTimes() As ReleaseTimeInDB()
        Get
            Return _releaseTimes
        End Get
        Protected Friend Set(value As ReleaseTimeInDB())
            _releaseTimes = value
        End Set
    End Property


    Public Sub New(ByVal customer As ImgdirCustomer)
        dateReceived = Date.Now
        Me.customer = customer
        releaseTimes = ImgDirCustDB.getReleaseTimes(customer)
        If releaseTimes.Length = 0 Then Throw New Exception("No Release Times Found for " + customer.prinergyCustName)
        Me.setDate()
    End Sub

    MustOverride Sub setDate()  ' set start and stop date

    Public MustOverride Function makeCollectionPrefix() As String ' create prfefix for collection job name


    Protected Friend Function parseTime(ByVal periodReleaseTime As ReleaseTimeInDB, _
                                        ByVal triggerTime As Date) As Date
        Dim enAu As New CultureInfo("en-AU")
        Dim parsingTimeString As String = (triggerTime.ToString("d", enAu) + Chr(32) + periodReleaseTime.Time.ToString("t", enAu))
        Return Date.Parse(parsingTimeString, enAu)
    End Function

End Class
