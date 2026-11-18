using BigData.Domain.Entities.Enums;

namespace BigData.Domain.Entities
{
    public class Parameters
    {
        public Guid Id { get; private set; }
        public ParameterType Type { get; private set; }
        public double Value { get; private set; }

        public static Parameters Create(Guid id, ParameterType type, double value)
        {
            return new Parameters
            {
                Id = id,
                Type = type,
                Value = value
            };
        }

        public void SetValue(double value)
        {
            Value = value;
        }
    }
}