using System.Collections.Generic;
using System;

[Serializable]
public class TriviaQuestion
{
    public string category;      
    public string question;
    public List<string> options;
    public int correctIndex;
}