using BigData.Domain.Entities.Enums;

namespace BigData.Domain.Entities
{
    public class Parameter
    {
        public Guid Id { get; private set; }
        public ParameterType Type { get; private set; }
        public double Value { get; private set; }

        public static Parameter Create(Guid id, string type, double value)
        {
            return new Parameter
            {
                Id = id,
                Type = type.ToLowerInvariant() switch
                {
                    "temp_dry" => ParameterType.Temperature,
                    "wind_speed" => ParameterType.WindSpeed,
                    "wind_dir" => ParameterType.WindDirection,
                    "pressure" => ParameterType.Pressure,
                    _ => throw new ArgumentException($"Invalid parameter type: {type}", nameof(type))
                },
                Value = value
            };
        }

        public void SetValue(double value)
        {
            Value = value;
        }
    }
}