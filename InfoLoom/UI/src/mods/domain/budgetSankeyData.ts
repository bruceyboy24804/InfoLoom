export interface BudgetSankeyData {
  totalIncome: number;
  totalExpenses: number;
  surplus: number;
  /** Per-source income values indexed by IncomeSource enum (0..13) */
  incomeValues: number[];
  /** Per-source expense values indexed by ExpenseSource enum (0..14), positive amounts */
  expenseValues: number[];
}

/** Matches the game's IncomeSource enum indices */
export const enum IncomeSource {
  ResidentialTaxes = 0,
  CommercialTaxes = 1,
  IndustrialTaxes = 2,
  OfficeTaxes = 3,
  ParkingFees = 4,
  RoadTolls = 5,
  Deathcare = 6,
  Garbage = 7,
  Healthcare = 8,
  Electricity = 9,
  Water = 10,
  Sewage = 11,
  TradeExport = 12,
  GovernmentSubsidy = 13,
}

/** Matches the game's ExpenseSource enum indices */
export const enum ExpenseSource {
  CityServices = 0,
  FireProtection = 1,
  Police = 2,
  Education = 3,
  Parks = 4,
  Roads = 5,
  Transportation = 6,
  Deathcare = 7,
  Healthcare = 8,
  Telecommunications = 9,
  Electricity = 10,
  Water = 11,
  Garbage = 12,
  TradeImport = 13,
  LoanInterest = 14,
}

/** Labels matching the game's incomeItems IDs for Sankey node display */
export const INCOME_LABELS: Record<IncomeSource, string> = {
  [IncomeSource.ResidentialTaxes]: 'Residential Taxes',
  [IncomeSource.CommercialTaxes]: 'Commercial Taxes',
  [IncomeSource.IndustrialTaxes]: 'Industrial Taxes',
  [IncomeSource.OfficeTaxes]: 'Office Taxes',
  [IncomeSource.ParkingFees]: 'Parking Fees',
  [IncomeSource.RoadTolls]: 'Road Tolls',
  [IncomeSource.Deathcare]: 'Deathcare Income',
  [IncomeSource.Garbage]: 'Garbage Income',
  [IncomeSource.Healthcare]: 'Healthcare Income',
  [IncomeSource.Electricity]: 'Electricity Income',
  [IncomeSource.Water]: 'Water Income',
  [IncomeSource.Sewage]: 'Sewage Income',
  [IncomeSource.TradeExport]: 'Service Trade',
  [IncomeSource.GovernmentSubsidy]: 'Government Subsidies',
};

/** Labels matching the game's expenseItems IDs for Sankey node display */
export const EXPENSE_LABELS: Record<ExpenseSource, string> = {
  [ExpenseSource.CityServices]: 'City Services',
  [ExpenseSource.FireProtection]: 'Fire Protection',
  [ExpenseSource.Police]: 'Police',
  [ExpenseSource.Education]: 'Education',
  [ExpenseSource.Parks]: 'Parks',
  [ExpenseSource.Roads]: 'Roads',
  [ExpenseSource.Transportation]: 'Transportation',
  [ExpenseSource.Deathcare]: 'Deathcare',
  [ExpenseSource.Healthcare]: 'Healthcare',
  [ExpenseSource.Telecommunications]: 'Telecommunications',
  [ExpenseSource.Electricity]: 'Electricity',
  [ExpenseSource.Water]: 'Water',
  [ExpenseSource.Garbage]: 'Garbage',
  [ExpenseSource.TradeImport]: 'Service Trade',
  [ExpenseSource.LoanInterest]: 'Loan Interest',
};

/** A resolved Sankey node ready for rendering */
export interface SankeyNode {
  id: string;
  label: string;
  value: number;
  type: 'income' | 'expense' | 'aggregate';
}

/** A resolved Sankey link ready for rendering */
export interface SankeyLink {
  source: string;
  target: string;
  value: number;
}

/** Full resolved Sankey graph derived from BudgetSankeyData */
export interface BudgetSankeyGraph {
  nodes: SankeyNode[];
  links: SankeyLink[];
}
