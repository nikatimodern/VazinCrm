using System;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

internal sealed class SepidarTransferValidationException(string message) : InvalidOperationException(message);
