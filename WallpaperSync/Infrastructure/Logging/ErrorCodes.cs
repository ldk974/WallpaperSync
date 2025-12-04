using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WallpaperSync.Infrastructure.Logging
{
    public static class ErrorCodes
    {
    // MAINFORM 1000–1099
    public const int MAIN_InitFailed = 1001;
    public const int MAIN_RunSafelyFailed = 1002;
    public const int MAIN_CatalogLoadFailed = 1003;
    public const int MAIN_PrepareWallpaperFailed = 1004;
    public const int MAIN_WorkflowApplyFailed = 1005;
    public const int MAIN_CopyViaTranscodedFailed = 1006;

    // STARTUPFORM 1100–1199
    public const int START_WorkflowApplyFailed = 1101;
    public const int START_CopyViaTranscodedFailed = 1102;

    // RESTOREFORM 1200–1299
    public const int RESTORE_DeleteFailed = 1201;
    public const int RESTORE_RestoreFailed = 1202;
    }
}
