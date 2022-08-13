// From keijiro's git.
// Link can be found here: https://gist.github.com/keijiro/1681052
// Unity Questions relating to this:
// https://answers.unity.com/questions/1670161/unlit-transparent-with-color-support.html
// https://answers.unity.com/questions/18173/unlit-transparent-shader.html
// https://answers.unity.com/questions/189695/add-color-property-to-unlit-alpha.html

Shader "Unlit/Transparent Colored" {
    Properties{
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
    }

        SubShader{
            Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}

            ZWrite Off
            Lighting Off
            Fog { Mode Off }

            Blend SrcAlpha OneMinusSrcAlpha

            Pass {
                Color[_Color]
                SetTexture[_MainTex] { combine texture * primary }
            }
    }
}