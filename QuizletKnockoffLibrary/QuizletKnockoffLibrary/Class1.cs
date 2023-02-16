namespace QuizletKnockoffLibrary
{
    public class Term
    {
        private string termText;
        private string termDefinition;
        private bool isKnown;

        Term(string termText, string termDefinition)
        {
            this.termText = termText;
            this.termDefinition = termDefinition;
            this.isKnown = false;
        }
    }

    public class StudySet
    {
        string setName;
        LinkedList<Term> termList = new LinkedList<Term>();
    }
}