export interface TabItem {
  id: string;
  label: string;
}

interface TabsProps {
  label: string;
  tabs: TabItem[];
  active: string;
  onChange: (id: string) => void;
}

/** Pestañas para alternar entre secciones de una misma página. */
export function Tabs({ label, tabs, active, onChange }: TabsProps) {
  return (
    <div className="tabs" role="tablist" aria-label={label}>
      {tabs.map((tab) => (
        <button
          key={tab.id}
          type="button"
          role="tab"
          id={`tab-${tab.id}`}
          aria-selected={tab.id === active}
          aria-controls={`panel-${tab.id}`}
          className="tab"
          onClick={() => onChange(tab.id)}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}
