using Campus.Contracts;

namespace Campus.Systems.Weather;

public static class WeatherVectors
{
    public static WeatherVec For(WeatherKind kind, float intensity)
    {
        WeatherRules.ValidateIntensity(intensity);
        float i = intensity;
        return kind switch
        {
            WeatherKind.Clear => new(.08f, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0),
            WeatherKind.FewClouds => new(.28f, .2f, 0, .04f, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0),
            WeatherKind.Cloudy => new(.58f, .4f, 0, .25f, 0, 0, 0, 0, 1.05f, 0, 0, 0, 0, 0),
            WeatherKind.Overcast => new(.92f, .7f, .1f, .85f, 0, 0, 0, 0, 1.2f, 0, .1f, 0, 0, 0),
            WeatherKind.Fog => new(.45f, .3f, 0, .25f, 0, 0, .15f, 0, 1.4f + i * 2, .3f + .7f * i, 0, 0, 0, 0),
            WeatherKind.LightRain => new(.8f, .65f, .15f, .65f, .12f + .3f * i, 0, .5f + .25f * i, .15f * i, 1.25f, .08f, .1f, 0, 0, 0),
            WeatherKind.Rain => new(.94f, .75f, .35f, .82f, .35f + .65f * i, 0, .7f + .3f * i, .2f + .6f * i, 1.4f, .1f, .25f * i, 0, 0, 0),
            WeatherKind.Thunderstorm => new(1, .9f, .9f, .95f, .5f + .5f * i, 0, 1, .45f + .5f * i, 1.5f, .1f, .6f + .4f * i, 0, 0, 0),
            WeatherKind.AfterRain => new(.35f, .35f, 0, .15f, 0, 0, .7f, .25f, 1.05f, .08f, 0, 0, 0, 0),
            WeatherKind.Snow => new(.88f, .6f, .05f, .65f, .2f + .8f * i, 1, .2f, 0, 1.2f, 0, .1f * i, .35f + .65f * i, 0, 0),
            WeatherKind.AfterSnow => new(.25f, .25f, 0, .1f, 0, 1, .15f, 0, 1, 0, 0, .65f, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public static WeatherVec Lerp(WeatherVec a, WeatherVec b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        float L(float x, float y) => x + (y - x) * t;
        return new(L(a.Cloud, b.Cloud), L(a.CloudType, b.CloudType), L(a.Cb, b.Cb), L(a.Overcast, b.Overcast),
            L(a.Precip, b.Precip), L(a.PrecipKind, b.PrecipKind), L(a.Wet, b.Wet), L(a.Puddle, b.Puddle),
            L(a.Fog, b.Fog), L(a.GroundFog, b.GroundFog), L(a.Storm, b.Storm), L(a.SnowAdd, b.SnowAdd),
            L(a.Rainbow, b.Rainbow), L(a.Lightning, b.Lightning));
    }
}
