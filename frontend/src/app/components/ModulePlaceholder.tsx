import { useId } from "react";

interface ModulePlaceholderProps {
  title: string;
  description: string;
}

/**
 * Vista provisional de un módulo.
 */
export function ModulePlaceholder({ title, description }: ModulePlaceholderProps) {
  const headingId = useId();

  return (
    <section aria-labelledby={headingId}>
      <h1 id={headingId}>{title}</h1>
      <p>{description}</p>
      <p role="status">Esta sección todavía está en desarrollo.</p>
    </section>
  );
}
