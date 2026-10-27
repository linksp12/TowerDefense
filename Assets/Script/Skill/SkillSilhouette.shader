Shader "LastOfTheTower/Skill Silhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnitySprites.cginc"
            fixed4 frag(v2f input) : SV_Target
            {
                fixed alpha = SampleSpriteTexture(input.texcoord).a * input.color.a;
                return fixed4(input.color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
