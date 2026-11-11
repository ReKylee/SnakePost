using LitMotion;
using SnakePost.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SnakePost.Menu
{
    /// <summary>
    /// The desk menu's UI: the daily-challenges folder that slides in from the screen edge and opens,
    /// and the loadout boxes with their perk picker. Layout is plain uGUI; this only drives the interactions.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [Header("Daily challenges")]
        [Tooltip("The folder at rest at the screen edge: its own upright render, so its shadow falls the right way. Hidden while the animated folder is out.")]
        [SerializeField] private GameObject _folderButton;
        [Tooltip("The animated folder. Hidden at rest; slides from where it sits (behind the button) to Folder Open Spot and opens.")]
        [SerializeField] private RectTransform _folder;
        [SerializeField] private SpriteFlipbook _folderFlipbook;
        [Tooltip("Where the open folder sits, how it's turned and how big it is (position, rotation, scale).")]
        [SerializeField] private RectTransform _folderOpenSpot;
        [Tooltip("The challenge list on the folder's open pages.")]
        [SerializeField] private CanvasGroup _challenges;
        [Tooltip("Full-screen dimmer behind the open folder. Tapping it closes the folder.")]
        [SerializeField] private GameObject _folderBlocker;
        [SerializeField, Min(.05f)] private float _folderSlideSeconds = .35f;

        [Header("Perks")]
        [SerializeField] private PerkDefinition[] _perks;
        [Tooltip("The perk shown inside each loadout box, in slot order.")]
        [SerializeField] private SpriteFlipbook[] _slotPerks;
        [SerializeField] private GameObject _perkPicker;
        [SerializeField] private TMP_Text _perkName;
        [SerializeField] private TMP_Text _perkDescription;
        [Tooltip("Inactive grid cell, copied once per perk.")]
        [SerializeField] private Button _perkCellTemplate;
        [SerializeField] private Button _chooseButton;

        // shortcut: equipped perks live in the menu until a profile/save exists; move them there with persistence.
        private PerkDefinition[] _equipped;
        private PerkDefinition _highlighted;
        private int _pickingSlot;
        private bool _folderOpen;
        private Vector3 _folderClosedPosition;
        private Quaternion _folderClosedRotation;
        private Vector3 _folderClosedScale;
        private MotionHandle _folderMotion;

        private void Awake()
        {
            _equipped = new PerkDefinition[_slotPerks.Length];
            foreach (var slot in _slotPerks)
                slot.gameObject.SetActive(false);

            _perkCellTemplate.gameObject.SetActive(false);
            foreach (var perk in _perks)
            {
                var cell = Instantiate(_perkCellTemplate, _perkCellTemplate.transform.parent);
                cell.name = perk.DisplayName;
                cell.gameObject.SetActive(true);
                cell.GetComponentInChildren<SpriteFlipbook>(true).SetFrames(perk.Turntable);
                cell.onClick.AddListener(() => Highlight(perk));
            }

            _perkPicker.SetActive(false);
            _folderBlocker.SetActive(false);
            _folder.gameObject.SetActive(false);
            SetChallengesVisible(false);
        }

        public void ToggleDailyChallenges()
        {
            if (_folderOpen)
                CloseDailyChallenges();
            else
                OpenDailyChallenges();
        }

        public void OpenDailyChallenges()
        {
            if (_folderOpen) return;

            _folderOpen = true;
            _folderButton.SetActive(false);
            _folder.gameObject.SetActive(true);
            _folderClosedPosition = _folder.position;
            _folderClosedRotation = _folder.localRotation;
            _folderClosedScale = _folder.localScale;
            _folderBlocker.SetActive(true);
            MoveFolder(_folderOpenSpot.position, _folderOpenSpot.localRotation, _folderOpenSpot.localScale,
                () => _folderFlipbook.Play(false, () => SetChallengesVisible(true)));
        }

        public void CloseDailyChallenges()
        {
            if (!_folderOpen) return;

            _folderOpen = false;
            SetChallengesVisible(false);
            _folderFlipbook.Play(true, () => MoveFolder(_folderClosedPosition, _folderClosedRotation, _folderClosedScale, () =>
            {
                _folderBlocker.SetActive(false);
                _folder.gameObject.SetActive(false);
                _folderButton.SetActive(true);
            }));
        }

        public void OpenPerkPicker(int slot)
        {
            _pickingSlot = slot;
            _perkPicker.SetActive(true);
            Highlight(_equipped[slot] != null ? _equipped[slot] : _perks.Length > 0 ? _perks[0] : null);
        }

        public void ClosePerkPicker() => _perkPicker.SetActive(false);

        public void ChooseHighlightedPerk()
        {
            if (_highlighted == null) return;

            _equipped[_pickingSlot] = _highlighted;
            var view = _slotPerks[_pickingSlot];
            view.gameObject.SetActive(true);
            view.SetFrames(_highlighted.Turntable);
            ClosePerkPicker();
        }

        // shortcut: PostShift and the shop don't exist yet; wire these buttons to them when they do.
        public void OpenPostShift() => Debug.Log("PostShift opens here once it exists.", this);
        public void OpenShop() => Debug.Log("The shop opens here once it exists.", this);

        private void Highlight(PerkDefinition perk)
        {
            _highlighted = perk;
            _perkName.text = perk != null ? perk.DisplayName : string.Empty;
            _perkDescription.text = perk != null ? perk.Description : string.Empty;
            _chooseButton.interactable = perk != null;
        }

        private void MoveFolder(Vector3 position, Quaternion rotation, Vector3 scale, System.Action onComplete)
        {
            if (_folderMotion.IsActive()) _folderMotion.Cancel();

            var fromPosition = _folder.position;
            var fromRotation = _folder.localRotation;
            var fromScale = _folder.localScale;
            _folderMotion = LMotion.Create(0f, 1f, _folderSlideSeconds)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(onComplete)
                .Bind(t =>
                {
                    _folder.position = Vector3.Lerp(fromPosition, position, t);
                    _folder.localRotation = Quaternion.Slerp(fromRotation, rotation, t);
                    _folder.localScale = Vector3.Lerp(fromScale, scale, t);
                });
        }

        private void SetChallengesVisible(bool visible)
        {
            _challenges.alpha = visible ? 1f : 0f;
            _challenges.blocksRaycasts = visible;
        }

        private void OnDestroy()
        {
            if (_folderMotion.IsActive()) _folderMotion.Cancel();
        }
    }
}
