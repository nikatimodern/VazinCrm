using System;
using System.Collections.Generic;
using CRM.Core.Domain.Attachments.Entities;

namespace CRM.EndPoint.FrontEnd.Models;

public class AttachmentModel {
    public List<Attachment> ListAttach { get; set; }
    public Guid Id { get; set; }
}