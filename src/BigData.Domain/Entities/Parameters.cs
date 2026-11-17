using BigData.Domain.Entities.Enums;

namespace BigData.Domain.Entities
{
    public class Parameters
    {
        public Guid Id { get; private set; }
        public ParameterType Type { get; private set; }
        public double Value { get; private set; }
    }
}
