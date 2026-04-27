using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.Core.Domain.CallRecordings.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Ai;
using Generics.Infrastructures.Sql;
using OpenAI;
using OpenAI.Models;
using OpenAI.Responses;
using Renci.SshNet;

namespace CRM.EndPoint.FrontEnd.Services.AI;

public class AudioToTextService {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CRMContextDb db;
    private readonly ViraAi _viraAi;
    private readonly GapgptAi _gapgptAi;

    private const string FtpHost = "192.168.1.150";
    private const string FtpUser = "root";
    private const string FtpPass = "123";
    //private const string TargetFilePath = @"wwwroot\recordings";

    public AudioToTextService(IHttpClientFactory httpClientFactory, CRMContextDb db)
    {
        _httpClientFactory = httpClientFactory;
        this.db = db;
    }


}