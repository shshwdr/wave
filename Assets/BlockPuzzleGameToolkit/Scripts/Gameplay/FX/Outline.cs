// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay.FX
{
    //显示高亮轮廓
    public class Outline : MonoBehaviour
    {
        private Image image;
        private Image image1;
        private RectTransform _rectTransform;

        private void Awake()
        {
            image = GetComponent<Image>();
            image1 = GetComponentInChildren<Image>();
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Init(Vector3 center, Vector2 sizeInLocalSpace, Color color)
        {
            _rectTransform.anchoredPosition = center;
            _rectTransform.sizeDelta = sizeInLocalSpace;
            image.color = color;
        }

        public void Play(Vector2 center, Vector2 size, Color white)
        {
            Init(center, size, white);
            image.DOFade(.5f, .5f).SetLoops(-1, LoopType.Yoyo);
            image1.DOFade(.5f, .5f).SetLoops(-1, LoopType.Yoyo);
        }
    }
}