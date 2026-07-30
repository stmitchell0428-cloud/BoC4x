using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BoC4x.Tests
{
    public class TechDetailPaneTests
    {
        [Test]
        public void AllTechIds_ExistInDatabase_WithDescriptionAndEffect()
        {
            foreach (ConfessionTechId id in Enum.GetValues(typeof(ConfessionTechId)))
            {
                Assert.IsTrue(
                    ConfessionTechDatabase.All.ContainsKey(id),
                    $"Missing database entry for {id}");

                var node = ConfessionTechDatabase.Get(id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(node.Name), $"{id} missing Name");
                Assert.IsFalse(string.IsNullOrWhiteSpace(node.Description), $"{id} missing Description");
                Assert.IsFalse(string.IsNullOrWhiteSpace(node.EffectSummary), $"{id} missing EffectSummary");
            }
        }

        [Test]
        public void AllTechOfflinePreviews_AreNonEmpty()
        {
            foreach (var id in ConfessionTechDatabase.All.Keys)
            {
                string preview = ConfessionTechDetailText.BuildOfflinePreview(id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(preview), $"{id} offline preview empty");
                Assert.IsTrue(preview.Contains(ConfessionTechDatabase.Get(id).Description),
                    $"{id} preview missing Description");
            }
        }

        [Test]
        public void DetailSidebar_ScrollsForLongContent_IncludingCelestialHarmonies()
        {
            var canvasGo = new GameObject("TechDetailTestCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(880f, 580f);

            var body = UiDetailPane.CreateSidebar(
                panel.transform,
                out var scroll,
                "Select a tech.",
                null);

            // Pin sidebar to a short fixed viewport so overflow is measurable in EditMode.
            var sidebar = scroll.transform.parent as RectTransform;
            Assert.IsNotNull(sidebar);
            sidebar.anchorMin = new Vector2(1f, 1f);
            sidebar.anchorMax = new Vector2(1f, 1f);
            sidebar.pivot = new Vector2(1f, 1f);
            sidebar.anchoredPosition = new Vector2(-8f, -44f);
            sidebar.sizeDelta = new Vector2(UiDetailPane.SidebarWidth, 220f);

            var scrollRectTransform = scroll.GetComponent<RectTransform>();
            scrollRectTransform.offsetMin = new Vector2(8f, 8f);
            scrollRectTransform.offsetMax = new Vector2(-8f, -8f);

            Canvas.ForceUpdateCanvases();

            var needingScroll = new List<string>();
            float viewportHeight = 0f;

            foreach (var id in ConfessionTechDatabase.All.Keys.OrderBy(k => k.ToString()))
            {
                string preview = ConfessionTechDetailText.BuildOfflinePreview(id);
                UiDetailPane.SetDetailText(body, scroll, preview);
                Canvas.ForceUpdateCanvases();

                viewportHeight = scroll.viewport.rect.height;
                float contentHeight = scroll.content.rect.height;

                Assert.Greater(contentHeight, 1f, $"{id}: content height not measured");
                Assert.AreEqual(1f, scroll.verticalNormalizedPosition, 0.02f,
                    $"{id}: should open scrolled to top");

                if (contentHeight > viewportHeight + 1f)
                    needingScroll.Add($"{ConfessionTechDatabase.Get(id).Name} ({contentHeight:F0}>{viewportHeight:F0})");
            }

            // Celestial Harmonies is the reported case; Civic track copy makes it long.
            string kepler = ConfessionTechDetailText.BuildOfflinePreview(ConfessionTechId.JohannesKepler);
            UiDetailPane.SetDetailText(body, scroll, kepler);
            Canvas.ForceUpdateCanvases();
            Assert.Greater(
                scroll.content.rect.height,
                scroll.viewport.rect.height,
                "Celestial Harmonies detail must overflow the sidebar so ScrollRect can reveal the rest.");

            Assert.IsTrue(
                scroll.vertical && scroll.viewport != null && scroll.content != null,
                "Detail sidebar must expose a vertical ScrollRect");

            // Raycast target required for mouse-wheel / drag scrolling.
            var hit = scroll.GetComponent<Image>();
            Assert.IsNotNull(hit);
            Assert.IsTrue(hit.raycastTarget);

            Debug.Log(
                $"Tech detail scroll check: viewport≈{viewportHeight:F0}px; " +
                $"{needingScroll.Count}/{ConfessionTechDatabase.All.Count} techs need scroll. " +
                $"Longest cases: {string.Join("; ", needingScroll.Take(12))}");

            Object.DestroyImmediate(canvasGo);
        }
    }
}
