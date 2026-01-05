using UnityEngine;
using UnityEngine.AI;

public class WorkerUnit : BasicUnit
{
	
//очень полезный файл. он нужен только для того, чтобы триггерить ClickHandler 
	/*
	public void GatherResource()
	{   
		if (unitAgent == null)
		{
			Debug.LogError($"Ошибка: NavMeshAgent не инициализирован у {gameObject.name}!");
			return;
		}

		if (!unitAgent.isOnNavMesh)
		{
			Debug.LogError($"Ошибка: {gameObject.name} не стоит на NavMesh!");
			return;
		}

		Vector3 target = FindClosest("Tree");
		if (target != transform.position)
		{
			unitAgent.SetDestination(target);
			Debug.Log($"{gameObject.name} идёт к ресурсу по координатам {target}");
		}
		else
		{
			Debug.LogWarning($"Не найдено ближайшее дерево для {gameObject.name}.");
		}
	}*/
}