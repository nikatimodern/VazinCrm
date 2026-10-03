using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sms;

// Dashboard
public sealed class FarazBalanceResponse {
    public FarazBalanceData data { get; set; } = new();
}

public sealed class FarazBalanceData {
    public string balance_amount { get; set; }
}

public sealed class FarazLinesResponse {
    public List<FarazLine> data { get; set; } = [];
}

public sealed class FarazLine {
    public string number { get; set; }
    public bool is_dedicated { get; set; }
    public decimal sms_cost { get; set; }
}

// Receive
public sealed class FarazReceiveResponse {
    public FarazReceiveData data { get; set; } = new();
}

public sealed class FarazReceiveData {
    public List<FarazReceiveItem> data { get; set; } = [];
    public int total { get; set; }
    public int last_page { get; set; }
}

public sealed class FarazReceiveItem {
    public long id { get; set; }
    public string from { get; set; }
    public string message { get; set; }
    public string to { get; set; }
    public DateTime created_at { get; set; }
    public DateTime received_at { get; set; }
    public bool seen { get; set; }
}

//Report
public sealed class FarazReportResponse {
    public FarazReportData data { get; set; } = new();
}

public sealed class FarazReportData {
    public List<FarazReportItem> data { get; set; } = [];
    public int total { get; set; }
    public int last_page { get; set; }
}

public sealed class FarazReportItem {
    public long id { get; set; }
    public string status { get; set; }
    public string type { get; set; }
    public DateTime schedule { get; set; }
    public string pattern_text { get; set; }

    public FarazReportSnapshot snapshot { get; set; }
    public FarazReportMetadata metadata { get; set; }

    public FarazReportLine overridden_line { get; set; }
    public FarazReportLine line { get; set; }
}

public sealed class FarazReportSnapshot {
    public DateTime? created_at { get; set; }
}

public sealed class FarazReportMetadata {
    public string recipient { get; set; }
    public string text { get; set; }
}

public sealed class FarazReportLine {
    public string number { get; set; }
}

// Response send sms
public class FarazSmsResponse {
    public string status { get; set; }
    public FarazSmsData data { get; set; }
    public string message { get; set; }
}

public class FarazSmsData {
    public long id { get; set; }
    public string type { get; set; } // pattern | simple
    public string status { get; set; } // success | error
    public FarazSmsResponseMetadata metadata { get; set; }
    public DateTime? schedule { get; set; }
}

public class FarazSmsResponseMetadata {
    public string line_number { get; set; }
    public string number_format { get; set; }

    // pattern sms
    public string code { get; set; }
    public Dictionary<string, string> attributes { get; set; }
    public string recipient { get; set; }

    // simple sms
    public string text { get; set; }
    public List<string> recipients { get; set; }
}