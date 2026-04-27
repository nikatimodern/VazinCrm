using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;

public class GetAllSortBySelectedVModel {
    public string Search { get; set; } = "";
    public int PassType { get; set; }
    public List<Guid> SelectedIds { get; set; } = [];
}