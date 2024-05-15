using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class Utils
{

#if UNITY_EDITOR
    public static readonly string externalDataPath = "./build/ExternalData";
#else
    public static readonly string externalDataPath = "./ExternalData";
#endif

    public static Color preGazedTarget = new Color(81/255, 135/255, 9/255);

    public static long GetTimeStamp()
    { 
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    public static System.Random rng;

    public static void SetRandomWithSeed(int seed)
    {
        rng = new System.Random(seed);
        UnityEngine.Random.InitState(seed);
    }

    public static List<T> ShuffleList<T>(List<T> list)
    {   // Fisher-Yates shuffle: http://stackoverflow.com/questions/273313/randomize-a-listt#1262619

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        return list;
    }

    public enum Lateralisation
    {
        left,
        right,
        comb
    }

    public static T GetRandomUnifInRange<T>(T[] range)
    {
        // https://stackoverflow.com/questions/8188784/how-can-i-subtract-two-generic-objects-t-t-in-c-sharp-example-datetime-d
        // T will be int, long or float, but I don't feel like writing 3 different methods

        dynamic start = (dynamic) range[0];
        dynamic end = (dynamic) range[1];

        return (T) (start + UnityEngine.Random.value * (end - start));
    }
    
    public static Vector3 GetRandomAroundSphere(float angleA, float angleB, Vector3 aroundPosition)
    {
        // https://stackoverflow.com/questions/64623448/random-rotation-on-a-3d-sphere-given-an-angle
        var v = UnityEngine.Random.value;
        var a = Mathf.Cos(Mathf.Deg2Rad * angleA);
        var b = Mathf.Cos(Mathf.Deg2Rad * angleB);

        float azimuth = v * 2.0F * Mathf.PI;
        float cosDistFromZenith = GetRandomUnifInRange( new []{ Mathf.Min(a, b), Mathf.Max(a, b) } );
        float sinDistFromZenith = Mathf.Sqrt(1.0F - cosDistFromZenith * cosDistFromZenith);
        Vector3 pqr = new Vector3(Mathf.Cos(azimuth) * sinDistFromZenith, Mathf.Sin(azimuth) * sinDistFromZenith, cosDistFromZenith);
        Vector3 rAxis = aroundPosition; // Vector3.up when around zenith
        Vector3 pAxis = Mathf.Abs(rAxis[0]) < 0.9 ? new Vector3(1F, 0F, 0F) : new Vector3(0F, 1F, 0F);
        Vector3 qAxis = Vector3.Normalize(Vector3.Cross(rAxis, pAxis));
        pAxis = Vector3.Cross(qAxis, rAxis);
        Vector3 position = pqr[0] * pAxis + pqr[1] * qAxis + pqr[2] * rAxis;
        return position;
    }

    public static readonly Dictionary<string, string> messages = new Dictionary<string, string>
    {
        {"calibStart",
            "Drücken Sie den Trigger, um die Kalibrierung zu starten."},
        {"failedCal",
            "Die <b>Kalibrierung</b> ist fehlgeschlagen.\n\n" +
            "Nehmen Sie sich eine Minute Zeit, \num Ihre Augen auszuruhen.\n" +
            "Drücken Sie den Trigger, wenn Sie wieder bereit sind."},
        {"successCal",
            "Die <b>Kalibrierung</b> war erfolgreich.\n" +
            "Drücken Sie den Trigger, um fortzufahren."},
        {"validStart",
            "validStart\nDrücken Sie den Trigger, um die Kalibrierung zu starten."}, // TODO - translate
        {"successVal",
            "successVal.\n" +
            "Drücken Sie den Trigger, um fortzufahren."}, // TODO - translate
        {"failedVal",
            "failVal.\n" +
            "Drücken Sie den Trigger, um fortzufahren."}, // TODO - translate
        // TODO - add test instructions
    };
}
