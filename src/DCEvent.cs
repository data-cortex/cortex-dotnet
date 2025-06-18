using System;
using System.Collections.Generic;

namespace DataCortex {
  public class DCAllEvent {
    public DateTime EventDateTime { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = "event";
    public int? EventIndex { get; set; }
    public string? GroupTag { get; set; }
    public string? Kingdom { get; set; }
    public string? Phylum { get; set; }
    public string? Class { get; set; }
    public string? Order { get; set; }
    public string? Family { get; set; }
    public string? Genus { get; set; }
    public string? Species { get; set; }
    public double? Float1 { get; set; }
    public double? Float2 { get; set; }
    public double? Float3 { get; set; }
    public double? Float4 { get; set; }
    public string? SpendCurrency { get; set; }
    public double? SpendAmount { get; set; }
    public string? SpendType { get; set; }
    public string? Network { get; set; }
    public string? Channel { get; set; }
    public string? FromTag { get; set; }
    public string? ToTag { get; set; }
    public List<string>? ToList { get; set; }
  }
  public class DCEvent : DCAllEvent {
    private new string? SpendCurrency { get; set; }
    private new double? SpendAmount { get; set; }
    private new string? SpendType { get; set; }
    private new string? Network { get; set; }
    private new string? Channel { get; set; }
    private new string? FromTag { get; set; }
    private new string? ToTag { get; set; }
    private new List<string>? ToList { get; set; }
  }
  public class DCEconomy : DCAllEvent {
    private new string? Network { get; set; }
    private new string? Channel { get; set; }
    private new string? FromTag { get; set; }
    private new string? ToTag { get; set; }
    private new List<string>? ToList { get; set; }
  }
  public class DCMessageSend : DCAllEvent {
    private new string? SpendCurrency { get; set; }
    private new double? SpendAmount { get; set; }
    private new string? SpendType { get; set; }
    private new string? ToTag { get; set; }
  }
  public class DCMessageClick : DCAllEvent {
    private new string? SpendCurrency { get; set; }
    private new double? SpendAmount { get; set; }
    private new string? SpendType { get; set; }
    private new List<string>? ToList { get; set; }
  }
  public class DCLogEvent {
    public DateTime EventDateTime { get; set; } = DateTime.UtcNow;
    public string? DeviceTag { get; set; }
    public string? UserTag { get; set; }
    public string? LogLine { get; set; }
    public string? Hostname { get; set; }
    public string? Filename { get; set; }
    public string? LogLevel { get; set; }
    public string? RemoteAddress { get; set; }
    public double? ResponseBytes { get; set; }
    public double? ResponseMs { get; set; }
  }
}
