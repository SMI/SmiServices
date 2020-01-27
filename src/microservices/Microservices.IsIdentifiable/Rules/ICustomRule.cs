namespace Microservices.IsIdentifiable.Rules
{
    public interface ICustomRule
    {
        RuleAction Apply(string fieldName, string fieldValue);
    }
}