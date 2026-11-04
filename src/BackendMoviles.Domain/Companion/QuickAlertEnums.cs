namespace BackendMoviles.Domain.Companion;

public enum QuickAlertType
{
    Craving = 1,
    TiredOrPain = 2,
    ComeHere = 3,
    Sos = 4
}

public enum QuickAlertStatus
{
    Pending = 1,
    Responded = 2
}

public enum QuickAlertResponse
{
    OnMyWay = 1,
    CallingNow = 2,
    Done = 3
}
