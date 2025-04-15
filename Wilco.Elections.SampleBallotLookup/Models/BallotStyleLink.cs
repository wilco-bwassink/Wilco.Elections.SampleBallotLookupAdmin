using System.ComponentModel.DataAnnotations;

public class BallotStyleLink
{
    [Key]
    public int StyleCode { get; set; }

    public int PrecinctID { get; set; }
    public string SplitID { get; set; }
    public string QALink { get; set; }
}
