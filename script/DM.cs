using UnityEngine;

namespace AmongUsFilterMod
{
    public static class DecoratorManager
    {
        public static void ApplyDecoration(PlayerControl player)
        {
            Transform oldDeco = player.transform.Find("PlayerDecoration");
            if (oldDeco != null) Object.Destroy(oldDeco.gameObject);

            GameObject deco = new GameObject("PlayerDecoration");
            deco.transform.SetParent(player.transform);
            deco.transform.localPosition = new Vector3(0, 0.7f, 0);

            SpriteRenderer sr = deco.AddComponent<SpriteRenderer>();
            
            // 全员统一装饰效果：微微粉色光晕
            sr.color = new Color(1.0f, 0.71f, 0.75f, 0.5f); // #FFB6C1 的 RGBA 表达
            deco.transform.localScale = new Vector3(0.4f, 0.4f, 1);
        }
    }
}