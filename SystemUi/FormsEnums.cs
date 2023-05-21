namespace T3.SystemUi;

/// <summary>
/// PopUpButtons enum, derived from System.Windows.Forms.MessageBoxButtons
/// </summary>
public enum PopUpButtons
{
    /// <summary>
    ///  Specifies that the message box contains an OK button. This field is
    ///  constant.
    /// </summary>
    Ok,

    /// <summary>
    ///  Specifies that the message box contains OK and Cancel buttons. This
    ///  field is constant.
    /// </summary>
    OkCancel,

    /// <summary>
    ///  Specifies that the message box contains Abort, Retry, and Ignore
    ///  buttons.
    ///  This field is constant.
    /// </summary>
    AbortRetryIgnore,

    /// <summary>
    ///  Specifies that the message box contains Yes, No, and Cancel buttons.
    ///  This field is constant.
    /// </summary>
    YesNoCancel,

    /// <summary>
    ///  Specifies that the
    ///  message box contains Yes and No buttons. This field is
    ///  constant.
    /// </summary>
    YesNo,

    /// <summary>
    ///  Specifies that the message box contains Retry and Cancel buttons.
    ///  This field is constant.
    /// </summary>
    RetryCancel,

    /// <summary>
    ///  Specifies that the message box contains Cancel, Try Again, and Continue buttons.
    ///  This field is constant.
    /// </summary>
    CancelTryContinue
}

public enum PopUpResult
{
    /// <summary>
    ///  Nothing is returned from the dialog box. This means that the modal
    ///  dialog continues running.
    /// </summary>
    None, 

    /// <summary>
    ///  The dialog box return value is OK (usually sent from a button labeled OK).
    /// </summary>
    Ok,

    /// <summary>
    ///  The dialog box return value is Cancel (usually sent from a button
    ///  labeled Cancel).
    /// </summary>
    Cancel,

    /// <summary>
    ///  The dialog box return value is Abort (usually sent from a button
    ///  labeled Abort).
    /// </summary>
    Abort,

    /// <summary>
    ///  The dialog box return value is Retry (usually sent from a button
    ///  labeled Retry).
    /// </summary>
    Retry,

    /// <summary>
    ///  The dialog box return value is Ignore (usually sent from a button
    ///  labeled Ignore).
    /// </summary>
    Ignore,

    /// <summary>
    ///  The dialog box return value is Yes (usually sent from a button
    ///  labeled Yes).
    /// </summary>
    Yes,

    /// <summary>
    ///  The dialog box return value is No (usually sent from a button
    ///  labeled No).
    /// </summary>
    No,

    /// <summary>
    /// The dialog box return value is Try Again (usually sent from a button labeled Try Again).
    /// </summary>
    TryAgain,

    /// <summary>
    /// The dialog box return value is Continue (usually sent from a button labeled Continue).
    /// </summary>
    Continue,
}