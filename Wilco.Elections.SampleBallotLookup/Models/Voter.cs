using System;
using System.ComponentModel.DataAnnotations;

public class Voter
{
    [Key]
    public long VUID { get; set; }

    public string TOWNID { get; set; }
    public string TNAME { get; set; }
    public string NMFIRST { get; set; }
    public string NMMID { get; set; }
    public string NMLAST { get; set; }
    public string NMSUFFIX { get; set; }
    public DateTime? DOB { get; set; }
    public DateTime? RDATE { get; set; }
    public string STATUS { get; set; }
    public string SREASON { get; set; }
    public DateTime? SDATE { get; set; }
    public string GENDER { get; set; }
    public string DEFFECTED { get; set; }
    public bool SUPPRESS { get; set; }
    public string SNAME { get; set; }
    public string ADSTR1 { get; set; }
    public string ADSTR2 { get; set; }
    public string ADNUM { get; set; }
    public string ADCITY { get; set; }
    public string ADST { get; set; }
    public string ADZIP4 { get; set; }
    public string ADZIP5 { get; set; }
    public string ADPREFIX { get; set; }
    public string ADSUFFIX { get; set; }
    public string ADSTYPE { get; set; }
    public string ADUTYPE { get; set; }
    public string ADPARCEL { get; set; }
    public string ADUNIT { get; set; }
    public string DESIG { get; set; }
    public string MADSTR1 { get; set; }
    public string MADSTR2 { get; set; }
    public string MADNUM { get; set; }
    public string MADCITY { get; set; }
    public string MADST { get; set; }
    public string MCOUNTRY { get; set; }
    public string MADZIP4 { get; set; }
    public string MADZIP5 { get; set; }
    public string MADUNIT { get; set; }
    public string MUTYPE { get; set; }
    public string MDESIG { get; set; }
    public bool FL_AD_OVERSEAS { get; set; }
    public string DASSIGNMENT { get; set; }
    public string PRECINCT { get; set; }
    public string COMM { get; set; }
    public string JP { get; set; }
    public string SBE { get; set; }
    public string STREP { get; set; }
    public string STSEN { get; set; }
    public string USREP { get; set; }
    public string CD_DIST_TYPE { get; set; }
    public string NB_DISTRICT { get; set; }
    public string SCHOOL { get; set; }
    public string CITY { get; set; }
    public string OTHER { get; set; }
    public string HISPANIC { get; set; }
    public DateTime? DTBALLOT { get; set; }
    public string PARTY { get; set; }
    public bool FPCAFLAG { get; set; }
    public string FBDELIVERY { get; set; }
    public string FAX { get; set; }
    public string MAIL { get; set; }
    public string FPARTY { get; set; }
}
