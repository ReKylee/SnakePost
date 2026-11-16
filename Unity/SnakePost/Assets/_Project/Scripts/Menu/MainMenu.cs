using System;
using _Project.Scripts.Upgrades;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Menu
{
    /// <summary>
    ///     The desk menu's UI: the daily-challenges folder that slides in from the screen edge and opens,
    ///     and the loadout boxes with their perk picker. Layout is plain uGUI; this only drives the interactions.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [Header("Daily challenges")]
        [Tooltip(
            "The folder at rest at the screen edge: its own upright render, so its shadow falls the right way. Hidden while the animated folder is out.")]
        [SerializeField]
        private GameObject folderButton;

        [Tooltip(
            "The animated folder. Hidden at rest; slides from where it sits (behind the button) to Folder Open Spot and opens.")]
        [SerializeField]
        private RectTransform folder;

        [SerializeField] private SpriteFlipbook folderFlipbook;

        [Tooltip("Where the open folder sits, how it's turned and how big it is (position, rotation, scale).")]
        [SerializeField]
        private RectTransform folderOpenSpot;

        [Tooltip("The challenge list on the folder's open pages.")] [SerializeField]
        private CanvasGroup challenges;

        [Tooltip("Full-screen dimmer behind the open folder. Tapping it closes the folder.")] [SerializeField]
        private GameObject folderBlocker;

        [SerializeField] [Min(.05f)] private float folderSlideSeconds = .35f;

        [Header("Perks")] [SerializeField] private PerkDefinition[] perks;

        [Tooltip("The perk shown inside each loadout box, in slot order.")] [SerializeField]
        private SpriteFlipbook[] slotPerks;

        [SerializeField] private GameObject perkPicker;
        [SerializeField] private TMP_Text perkName;
        [SerializeField] private TMP_Text perkDescription;

        [Tooltip("Inactive grid cell, copied once per perk.")] [SerializeField]
        private Button perkCellTemplate;

        [SerializeField] private Button chooseButton;

        // shortcut: equipped perks live in the menu until a profile/save exists; move them there with persistence.
        private PerkDefinition[] _equipped;
        private Vector3 _folderClosedPosition;
        private Quaternion _folderClosedRotation;
        private Vector3 _folderClosedScale;
        private MotionHandle _folderMotion;
        private bool _folderOpen;
        private PerkDefinition _highlighted;
        private int _pickingSlot;

        private void Awake()
        {
            _equipped = new PerkDefinition[slotPerks.Length];

            foreach (SpriteFlipbook slot in slotPerks)
            {
                slot.gameObject.SetActive(false);
            }

            perkCellTemplate.gameObject.SetActive(false);

            foreach (PerkDefinition perk in perks)
            {
                Button cell = Instantiate(perkCellTemplate, perkCellTemplate.transform.parent);
                cell.name = perk.DisplayName;
                cell.gameObject.SetActive(true);
                cell.GetComponentInChildren<SpriteFlipbook>(true).SetFrames(perk.Turntable);
                cell.onClick.AddListener(() => Highlight(perk));
            }

            perkPicker.SetActive(false);
            folderBlocker.SetActive(false);
            folder.gameObject.SetActive(false);
            SetChallengesVisible(false);
        }

        private void OnDestroy()
        {
            if (_folderMotion.IsActive()) _folderMotion.Cancel();
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
            folderButton.SetActive(false);
            folder.gameObject.SetActive(true);
            _folderClosedPosition = folder.position;
            _folderClosedRotation = folder.localRotation;
            _folderClosedScale = folder.localScale;
            folderBlocker.SetActive(true);

            MoveFolder(
                folderOpenSpot.position, folderOpenSpot.localRotation, folderOpenSpot.localScale,
                () => folderFlipbook.Play(false, () => SetChallengesVisible(true)));
        }

        public void CloseDailyChallenges()
        {
            if (!_folderOpen) return;

            _folderOpen = false;
            SetChallengesVisible(false);

            folderFlipbook.Play(
                true, () => MoveFolder(
                    _folderClosedPosition, _folderClosedRotation, _folderClosedScale, () =>
                    {
                        folderBlocker.SetActive(false);
                        folder.gameObject.SetActive(false);
                        folderButton.SetActive(true);
                    }));
        }

        public void OpenPerkPicker(int slot)
        {
            _pickingSlot = slot;
            perkPicker.SetActive(true);
            Highlight(_equipped[slot] != null ? _equipped[slot] : perks.Length > 0 ? perks[0] : null);
        }

        public void ClosePerkPicker() => perkPicker.SetActive(false);

        public void ChooseHighlightedPerk()
        {
            if (_highlighted == null) return;

            _equipped[_pickingSlot] = _highlighted;
            SpriteFlipbook view = slotPerks[_pickingSlot];
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
            perkName.text = perk != null ? perk.DisplayName : string.Empty;
            perkDescription.text = perk != null ? perk.Description : string.Empty;
            chooseButton.interactable = perk != null;
        }

        private void MoveFolder(Vector3 position, Quaternion rotation, Vector3 scale, Action onComplete)
        {
            if (_folderMotion.IsActive()) _folderMotion.Cancel();

            Vector3 fromPosition = folder.position;
            Quaternion fromRotation = folder.localRotation;
            Vector3 fromScale = folder.localScale;

            _folderMotion = LMotion.Create(0f, 1f, folderSlideSeconds)
                                   .WithEase(Ease.OutCubic)
                                   .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                                   .WithOnComplete(onComplete)
                                   .Bind(t =>
                                   {
                                       folder.position = Vector3.Lerp(fromPosition, position, t);
                                       folder.localRotation = Quaternion.Slerp(fromRotation, rotation, t);
                                       folder.localScale = Vector3.Lerp(fromScale, scale, t);
                                   });
        }

        private void SetChallengesVisible(bool visible)
        {
            challenges.alpha = visible ? 1f : 0f;
            challenges.blocksRaycasts = visible;
        }
    }
}