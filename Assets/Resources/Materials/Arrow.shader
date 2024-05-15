Shader"Unlit/ff"
{
	Properties
	{
		_Color ("Colour", Color) = (1,1,1,1)
	}
	SubShader
	{
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

Cull Off
ZWrite Off
ZTest Always
blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			// https://iquilezles.org/articles/distfunctions2d/
			float sdBox(in float2 p, in float2 b)
			{
				float2 d = abs(p) - b;
				return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
			}

			float sdEquilateralTriangle(in float2 p, in float r)
			{
				const float k = sqrt(3.0);
				p.x = abs(p.x) - r;
				p.y = p.y + r / k;
				if (p.x + k * p.y > 0.0)
				{
					p = float2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
				}
				p.x -= clamp(p.x, -2.0 * r, 0.0);
				return -length(p) * sign(p.y);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = 2 * i.uv.xy - 1. + float2(0, -.2);
	
				float arrowTip = sdEquilateralTriangle(uv + float2(.0, -.1), .35);
				float arrowBody = sdBox((uv + float2(.0, .35)) * float2(6, 1), .65);
	
				if (arrowTip < .025 || arrowBody < .05)
				{
					return fixed4(_Color.xyz, 1);
				}
				else if (arrowTip < .035 || arrowBody < .08)
				{
					return fixed4(_Color.xyz -.15, 1);
				}
	
				return 0;
			}
			ENDCG
		}
	}
}
