using Koy.Core.Audio;
using Xunit;

namespace Koy.Core.Tests;

public class SpectrumAnalyzerTests
{
    private const int Rate = 48_000;

    private static float[] Sine(double hz, double amplitude = 0.5, int count = SpectrumAnalyzer.FftSize)
    {
        var samples = new float[count];
        for (var i = 0; i < count; i++)
            samples[i] = (float)(amplitude * Math.Sin(2 * Math.PI * hz * i / Rate));
        return samples;
    }

    private static int LoudestBand(SpectrumAnalyzer analyzer)
    {
        var levels = analyzer.Levels;
        var best = 0;
        for (var i = 1; i < levels.Length; i++)
        {
            if (levels[i] > levels[best])
                best = i;
        }
        return best;
    }

    private static SpectrumAnalyzer Run(float[] samples, int frames = 8)
    {
        var analyzer = new SpectrumAnalyzer(24);
        for (var i = 0; i < frames; i++)
        {
            analyzer.AddSamples(samples);
            analyzer.Update(Rate);
        }
        return analyzer;
    }

    [Fact]
    public void Bas_sol_tiz_sag_bantta_cikar()
    {
        var bass = LoudestBand(Run(Sine(80)));
        var mid = LoudestBand(Run(Sine(1000)));
        var treble = LoudestBand(Run(Sine(8000)));

        Assert.True(bass < mid, $"bas {bass}, orta {mid}");
        Assert.True(mid < treble, $"orta {mid}, tiz {treble}");
        Assert.True(bass <= 3);
        Assert.True(treble >= 18);
    }

    [Fact]
    public void Sessizlikte_cubuklar_iner()
    {
        var analyzer = Run(Sine(1000));
        Assert.False(analyzer.IsSilent);

        for (var i = 0; i < 60; i++)
        {
            analyzer.AddSilence(SpectrumAnalyzer.FftSize);
            analyzer.Update(Rate);
        }

        Assert.True(analyzer.IsSilent);
    }

    [Fact]
    public void Duzeyler_sifir_ile_bir_arasinda()
    {
        var analyzer = Run(Sine(440, amplitude: 1.0));
        foreach (var level in analyzer.Levels)
            Assert.InRange(level, 0f, 1f);
    }

    [Fact]
    public void Yuksek_ses_kisik_sesten_uzun_cubuk_verir()
    {
        var loud = Run(Sine(1000, 0.8));
        var quiet = Run(Sine(1000, 0.01));
        Assert.True(loud.Levels[LoudestBand(loud)] > quiet.Levels[LoudestBand(quiet)]);
    }

    [Theory]
    [InlineData(44_100)]
    [InlineData(48_000)]
    [InlineData(96_000)]
    [InlineData(8_000)]
    public void Bant_sinirlari_artan_ve_sinirda(int rate)
    {
        var edges = SpectrumAnalyzer.BandEdges(24, rate);
        Assert.Equal(25, edges.Length);
        for (var i = 1; i < edges.Length; i++)
            Assert.True(edges[i] > edges[i - 1] || edges[i] == SpectrumAnalyzer.FftSize / 2);
        Assert.True(edges[0] >= 1);
        Assert.True(edges[^1] <= SpectrumAnalyzer.FftSize / 2);
    }

    [Fact]
    public void Kisa_parcalar_halka_bellekte_birikir()
    {
        var analyzer = new SpectrumAnalyzer(24);
        var sine = Sine(1000);
        for (var frame = 0; frame < 8; frame++)
        {
            for (var i = 0; i < sine.Length; i += 128)
                analyzer.AddSamples(sine.AsSpan(i, 128));
            analyzer.Update(Rate);
        }

        Assert.False(analyzer.IsSilent);
    }

    [Fact]
    public void Tiz_bantlar_egimle_yukseltilir()
    {
        var tilt = SpectrumAnalyzer.BandTilt(SpectrumAnalyzer.BandEdges(24, Rate), Rate);
        for (var i = 1; i < tilt.Length; i++)
            Assert.True(tilt[i] >= tilt[i - 1]);
        Assert.True(tilt[0] < 0);
        Assert.True(tilt[^1] > 10);
    }

    [Fact]
    public void Gecersiz_bant_sayisi_reddedilir() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectrumAnalyzer(0));
}
