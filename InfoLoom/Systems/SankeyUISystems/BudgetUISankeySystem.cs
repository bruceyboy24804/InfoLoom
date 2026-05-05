using System;
using Colossal.UI.Binding;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using InfoLoomTwo.Extensions;
using Unity.Entities;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.SankeyUISystems
{
    public partial class BudgetUISankeySystem : ExtendedUISystemBase
    {
        private const string kGroup = "InfoLoomTwo";
        private const string kBudgetSankeyOpen = "BudgetSankeyOpen";

        private PrefabSystem m_PrefabSystem;
        private ICityServiceBudgetSystem m_CityServiceBudgetSystem;
        private EntityQuery m_ConfigQuery;

        private ValueBinding<bool> m_OpenBinding;
        private RawValueBinding m_BudgetSankeyData;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CityServiceBudgetSystem = World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
            m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIEconomyConfigurationData>());

            m_OpenBinding = new ValueBinding<bool>(kGroup, kBudgetSankeyOpen, false);
            AddBinding(m_OpenBinding);
            AddBinding(new TriggerBinding<bool>(kGroup, kBudgetSankeyOpen, SetOpen));

            AddBinding(m_BudgetSankeyData = new RawValueBinding(kGroup, "budgetSankeyData", WriteSankeyData));
        }

        private void SetOpen(bool open) => m_OpenBinding.Update(open);

        [Preserve]
        protected override void OnUpdate()
        {
            m_BudgetSankeyData.Update();
        }

        private void WriteSankeyData(IJsonWriter writer)
        {
            UIEconomyConfigurationPrefab config = m_PrefabSystem.GetSingletonPrefab<UIEconomyConfigurationPrefab>(m_ConfigQuery);

            int totalIncome = m_CityServiceBudgetSystem.GetTotalIncome();
            int totalExpenses = m_CityServiceBudgetSystem.GetTotalExpenses();
            int surplus = totalIncome - totalExpenses;

            // --- Aggregate income by display group ---
            BudgetItem<IncomeSource>[] incomeItems = config.m_IncomeItems;
            int[] incomeGroupValues = new int[incomeItems.Length];
            for (int i = 0; i < incomeItems.Length; i++)
            {
                int sum = 0;
                foreach (IncomeSource source in incomeItems[i].m_Sources)
                    sum += m_CityServiceBudgetSystem.GetIncome(source);
                incomeGroupValues[i] = sum;
            }

            // --- Aggregate expenses by display group ---
            // GetExpense returns negative values; negate to get positive amounts
            BudgetItem<ExpenseSource>[] expenseItems = config.m_ExpenseItems;
            int[] expenseGroupValues = new int[expenseItems.Length];
            for (int i = 0; i < expenseItems.Length; i++)
            {
                int sum = 0;
                foreach (ExpenseSource source in expenseItems[i].m_Sources)
                    sum += -m_CityServiceBudgetSystem.GetExpense(source);
                expenseGroupValues[i] = sum;
            }

            // --- Count active links for pre-sizing ---
            int activeIncome = 0;
            for (int i = 0; i < incomeGroupValues.Length; i++)
                if (incomeGroupValues[i] > 0) activeIncome++;
            int activeExpense = 0;
            for (int i = 0; i < expenseGroupValues.Length; i++)
                if (expenseGroupValues[i] > 0) activeExpense++;

            int nodeCount = incomeItems.Length + expenseItems.Length + 3; // +3: Revenue, Expense, Surplus
            int linkCount = activeIncome
                + (totalExpenses > 0 ? 1 : 0)
                + (surplus > 0 ? 1 : 0)
                + activeExpense;

            // --- Write JSON ---
            writer.TypeBegin("BudgetSankeyData");

            writer.PropertyName("totalIncome");
            writer.Write(totalIncome);

            writer.PropertyName("totalExpenses");
            writer.Write(totalExpenses);

            writer.PropertyName("surplus");
            writer.Write(Math.Max(0, surplus));

            // Nodes
            writer.PropertyName("nodes");
            writer.ArrayBegin((uint)nodeCount);

            for (int i = 0; i < incomeItems.Length; i++)
            {
                writer.TypeBegin("SankeyNode");
                writer.PropertyName("id");
                writer.Write(incomeItems[i].m_ID);
                writer.PropertyName("color");
                writer.Write(incomeItems[i].m_Color);
                writer.PropertyName("value");
                writer.Write(incomeGroupValues[i]);
                writer.PropertyName("type");
                writer.Write("income");
                writer.TypeEnd();
            }

            // Revenue aggregate node
            writer.TypeBegin("SankeyNode");
            writer.PropertyName("id");
            writer.Write("Revenue");
            writer.PropertyName("color");
            writer.Write(new UnityEngine.Color(0.30f, 0.69f, 0.31f)); // green
            writer.PropertyName("value");
            writer.Write(totalIncome);
            writer.PropertyName("type");
            writer.Write("aggregate");
            writer.TypeEnd();

            // Expense aggregate node
            writer.TypeBegin("SankeyNode");
            writer.PropertyName("id");
            writer.Write("Expense");
            writer.PropertyName("color");
            writer.Write(new UnityEngine.Color(0.96f, 0.26f, 0.21f)); // red
            writer.PropertyName("value");
            writer.Write(totalExpenses);
            writer.PropertyName("type");
            writer.Write("aggregate");
            writer.TypeEnd();

            // Surplus node
            writer.TypeBegin("SankeyNode");
            writer.PropertyName("id");
            writer.Write("Surplus");
            writer.PropertyName("color");
            writer.Write(new UnityEngine.Color(0.13f, 0.59f, 0.95f)); // blue
            writer.PropertyName("value");
            writer.Write(Math.Max(0, surplus));
            writer.PropertyName("type");
            writer.Write("aggregate");
            writer.TypeEnd();

            for (int i = 0; i < expenseItems.Length; i++)
            {
                writer.TypeBegin("SankeyNode");
                writer.PropertyName("id");
                writer.Write(expenseItems[i].m_ID);
                writer.PropertyName("color");
                writer.Write(expenseItems[i].m_Color);
                writer.PropertyName("value");
                writer.Write(expenseGroupValues[i]);
                writer.PropertyName("type");
                writer.Write("expense");
                writer.TypeEnd();
            }

            writer.ArrayEnd();

            // Links
            writer.PropertyName("links");
            writer.ArrayBegin((uint)linkCount);

            // Income sources → Revenue
            for (int i = 0; i < incomeItems.Length; i++)
            {
                if (incomeGroupValues[i] <= 0) continue;
                writer.TypeBegin("SankeyLink");
                writer.PropertyName("source");
                writer.Write(incomeItems[i].m_ID);
                writer.PropertyName("target");
                writer.Write("Revenue");
                writer.PropertyName("value");
                writer.Write(incomeGroupValues[i]);
                writer.TypeEnd();
            }

            // Revenue → Expense
            if (totalExpenses > 0)
            {
                writer.TypeBegin("SankeyLink");
                writer.PropertyName("source");
                writer.Write("Revenue");
                writer.PropertyName("target");
                writer.Write("Expense");
                writer.PropertyName("value");
                writer.Write(totalExpenses);
                writer.TypeEnd();
            }

            // Revenue → Surplus
            if (surplus > 0)
            {
                writer.TypeBegin("SankeyLink");
                writer.PropertyName("source");
                writer.Write("Revenue");
                writer.PropertyName("target");
                writer.Write("Surplus");
                writer.PropertyName("value");
                writer.Write(surplus);
                writer.TypeEnd();
            }

            // Expense → expense categories
            for (int i = 0; i < expenseItems.Length; i++)
            {
                if (expenseGroupValues[i] <= 0) continue;
                writer.TypeBegin("SankeyLink");
                writer.PropertyName("source");
                writer.Write("Expense");
                writer.PropertyName("target");
                writer.Write(expenseItems[i].m_ID);
                writer.PropertyName("value");
                writer.Write(expenseGroupValues[i]);
                writer.TypeEnd();
            }

            writer.ArrayEnd();

            writer.TypeEnd();
        }
    }
}