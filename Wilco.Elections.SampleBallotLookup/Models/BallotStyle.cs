using System.ComponentModel.DataAnnotations;

public class BallotStyle
{
    [Key]
    public long VUID { get; set; }

    public int PCT_CODE { get; set; }
    public string LABEL { get; set; }
}

