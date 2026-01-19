using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
	public float health;
	public float maxHealth = 100;

	void Start()
	{
		health = 100;
	}
	public void TakeDamage(int amount)
	{
		health -= amount;
		Debug.Log($"{gameObject.name} получил {amount} урона. Осталось {health} HP.");

		if (health <= 0)
		{
			Die();
		}
	}

	void Die()
	{
		Debug.Log($"{gameObject.name} уничтожен!");
		Destroy(gameObject);
	}
}
