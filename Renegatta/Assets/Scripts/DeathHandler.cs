// DeathHandler.cs (listens to PlayerHealth.OnDeath)
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DeathHandler : MonoBehaviour
{
public PlayerHealth health;


[Header("Disable on death")]
public Behaviour[] componentsToDisable;


[Header("Death UI")]
public GameObject shipwreckedUIPanel;


[Header("Death Animation")]
public float deathTiltZ = 20f;
public float deathTiltX = 10f;
public float sinkDistance = 5f;
public float sinkDuration = 3f;
public float deathDelayBeforeReload = 2f;


private void Awake()
{
if (health != null)
health.OnDeath += HandleDeath;
}


private void HandleDeath()
{
foreach (var c in componentsToDisable)
{
if (c != null) c.enabled = false;
}


if (shipwreckedUIPanel != null)
shipwreckedUIPanel.SetActive(true);


StartCoroutine(DeathSequence());
}


private IEnumerator DeathSequence()
{
Vector3 startPos = transform.localPosition;
Quaternion startRot = transform.localRotation;
Quaternion targetRot = Quaternion.Euler(deathTiltX, startRot.eulerAngles.y, startRot.eulerAngles.z + deathTiltZ);
Vector3 targetPos = startPos - Vector3.up * sinkDistance;


float elapsed = 0f;
while (elapsed < sinkDuration)
{
float t = elapsed / sinkDuration;
transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
elapsed += Time.deltaTime;
yield return null;
}


transform.localRotation = targetRot;
transform.localPosition = targetPos;


yield return new WaitForSeconds(deathDelayBeforeReload);


Scene active = SceneManager.GetActiveScene();
SceneManager.LoadScene(active.name);
}
}