﻿using UnityEngine;
using UnityEditor;
using RuntimeSounds.Editor.UI.Windows;
using RuntimeSounds.Api;
using RuntimeSounds.V1;

[CustomEditor(typeof(RuntimeAudio))]
public class RuntimeAudioEditor : Editor
{
    public EditorAudioClip selectedClipData = new EditorAudioClip();

    private GameObject previewGameObject;

    private EditorApplication.CallbackFunction updateCallback;
    private EditorApplication.CallbackFunction repaintCallback;
    private double nextRepaintTime = 0;

    void OnEnable()
    {
        if (!previewGameObject)
        {
            previewGameObject = EditorUtility.CreateGameObjectWithHideFlags("PreviewAudio", HideFlags.HideAndDontSave);
        }
    }

    public override void OnInspectorGUI()
    {
        var myTarget = (RuntimeAudio)target;
        var icon = Resources.Load<Texture2D>("RuntimeSound");
        var script = MonoScript.FromMonoBehaviour((RuntimeAudio)target);

        if (script != null && icon != null)
        {
            EditorGUIUtility.SetIconForObject(script, icon);
            AssetDatabase.SaveAssets();
        }

        if (repaintCallback == null)
        {
            repaintCallback = () =>
            {
                if (EditorApplication.timeSinceStartup > nextRepaintTime)
                {
                    nextRepaintTime = EditorApplication.timeSinceStartup + 0.05;

                    Repaint();
                }
            };

            EditorApplication.update += repaintCallback;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Audio Clip", new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            }
        }, GUILayout.Width(66));

        EditorGUILayout.EndHorizontal();

        if (myTarget.selectedAsset != null && !string.IsNullOrEmpty(myTarget.selectedAsset.Url))
        {
            RenderAudioClip(myTarget.selectedAsset, selectedClipData);
        }
        else
        {
            EditorGUILayout.BeginVertical(new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 6)
            });

            EditorGUILayout.BeginHorizontal();

            // Display the audio clip name

            EditorGUILayout.BeginVertical();

            Rect waveformRect = GUILayoutUtility.GetRect(100, 10, new GUIStyle()
            {
                margin = new RectOffset(0, 0, 7, 0)
            });
            GUI.DrawTexture(waveformRect, new Texture2D(200, 10));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("None", GUILayout.Width(70));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("00:00:00", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Play Button
            GUI.enabled = false;
            if (GUILayout.Button("▶ Play", GUILayout.Width(60), GUILayout.Height(34)))
            {
            }
            GUI.enabled = true;

            if (GUILayout.Button("Set", GUILayout.Width(60), GUILayout.Height(34)))
            {
                AudioSearchWindow.Open(this, myTarget);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.LabelField("Audio Settings", new GUIStyle()
        {
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            }
        });

        myTarget.playOnLoad = EditorGUILayout.Toggle("Play on Load", myTarget.playOnLoad);
        myTarget.audioSource = (AudioSource)EditorGUILayout.ObjectField("Audio Source", myTarget.audioSource, typeof(AudioSource), true);
        EditorGUILayout.LabelField("Note that the AudioClip property of the Audio Source component set above will be overriden by this component.", new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
        });

        if (GUI.changed)
        {
            EditorUtility.SetDirty(myTarget);
        }
    }

    private void RenderAudioClip (Asset asset, EditorAudioClip clipData)
    {
        EditorGUILayout.BeginVertical(new GUIStyle()
        {
            margin = new RectOffset(0, 0, 0, 10)
        });

        EditorAudio.DrawAudioClip(asset, clipData, previewGameObject, updateCallback);

        EditorGUILayout.BeginVertical(new GUIStyle()
        {
            margin = new RectOffset(0, 0, 0, 8)
        });

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        EditorAudio.DrawPlayButton(asset, clipData, previewGameObject, updateCallback, true);

        if (GUILayout.Button("Change", GUILayout.Width(60), GUILayout.Height(34)))
        {
            var myTarget = (RuntimeAudio)target;

            AudioSearchWindow.Open(this, myTarget);
        }

        if (!AudioCache.Exists(asset.Id))
        {
            if (clipData.caching)
                GUI.enabled = false;

            if (GUILayout.Button(clipData.caching ? "Caching" : "Cache", GUILayout.Width(64), GUILayout.Height(34)))
            {
                clipData.caching = true;

                RuntimeSoundsSdk.DownloadFileIntoMemoryAsync(asset.Url).ContinueWith(async p =>
                {
                    clipData.caching = false;

                    if (p.Result == null)
                        return;

                    await AudioCache.Save(p.Result, asset.Id);
                });
            }

            GUI.enabled = true;
        } else
        {
            if (GUILayout.Button("Uncache", GUILayout.Width(64), GUILayout.Height(34)))
            {
                AudioCache.Remove(asset.Id);
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void OnDisable()
    {
        if (previewGameObject) DestroyImmediate(previewGameObject.gameObject);

        AudioSearchWindow.ForceClose();
    }
}