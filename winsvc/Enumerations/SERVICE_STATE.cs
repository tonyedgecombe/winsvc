// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace frogmore.winsvc.Enumerations
{
    public enum SERVICE_STATE : uint
    {
        SERVICE_STOPPED          = 0x00000001,
        SERVICE_START_PENDING    = 0x00000002,
        SERVICE_STOP_PENDING     = 0x00000003,
        SERVICE_RUNNING          = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING    = 0x00000006,
        SERVICE_PAUSED           = 0x00000007,
    }
}