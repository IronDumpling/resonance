using UnityEngine;

namespace Resonance.Utilities.Waves
{
    public static class WaveformGenerator
    {
        public static float[] Generate(WaveformType type, int resolution)
        {
            float[] table = new float[resolution];
            switch (type)
            {
                case WaveformType.Sine:
                    GenerateSine(table, resolution);
                    break;
                case WaveformType.Square:
                    GenerateSquare(table, resolution); // Default 50% duty cycle
                    break;
                case WaveformType.Triangle:
                    GenerateTriangle(table, resolution);
                    break;
                case WaveformType.Sawtooth:
                    GenerateSawtooth(table, resolution); // Default rising sawtooth
                    break;
                case WaveformType.Custom: // Custom means it's a result of operations
                    Debug.LogWarning("WaveformGenerator: Custom waveform type not implemented.");
                    break;
                default:
                    // Maybe return silence or a sine wave as default
                    GenerateSine(table, resolution);
                    break;
            }
            return table;
        }

        private static void GenerateSine(float[] table, int resolution)
        {
            for (int i = 0; i < resolution; i++)
            {
                table[i] = Mathf.Sin((float)i / resolution * 2f * Mathf.PI);
            }
        }

        private static void GenerateSquare(float[] table, int resolution, float dutyCycle = 0.5f)
        {
            int splitPoint = Mathf.Clamp(Mathf.RoundToInt(resolution * dutyCycle), 0, resolution);
            for (int i = 0; i < resolution; i++)
            {
                table[i] = (i < splitPoint) ? 1.0f : -1.0f;
            }
        }

        private static void GenerateTriangle(float[] table, int resolution)
        {
            for (int i = 0; i < resolution; i++)
            {
                float phase = (float)i / resolution;
                if (phase < 0.5f)
                {
                    table[i] = -1.0f + 4.0f * phase; // Rises from -1 to 1 over first half
                }
                else
                {
                    table[i] = 1.0f - 4.0f * (phase - 0.5f); // Falls from 1 to -1 over second half
                }
            }
        }

        private static void GenerateSawtooth(float[] table, int resolution) // Rising Saw
        {
            for (int i = 0; i < resolution; i++)
            {
                table[i] = -1.0f + 2.0f * ((float)i / resolution);
            }
        }
        // You can add more generators (e.g., falling sawtooth) or parameterized ones
         public static float[] GenerateSquareParameterized(int resolution, float dutyCycle)
         {
             float[] table = new float[resolution];
             GenerateSquare(table, resolution, dutyCycle);
             return table;
         }
    }
}