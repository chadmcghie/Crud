﻿namespace Api.Dto
{    
    internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }

    internal record UserDto(Guid Id, string Name);
}
