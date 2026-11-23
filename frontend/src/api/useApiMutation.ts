import { useCallback, useRef, useState } from "react";                                                                                                                          
                                                                                                                                                                                              
export type MutationState<T> =                                                                                                                                           
  | { status: "idle" }                                                                                                                                                    
  | { status: "loading" }                                                                                                                                                   
  | { status: "error"; error: unknown }                                                                                                                                  
  | { status: "success"; data: T };                                                                                                                                                      
                                                                                                                                                                         
export type MutationOutcome<T> = { ok: true; data: T } | { ok: false };                                                                    
                                                                                                                                                                                              
export interface MutationResult<A extends unknown[], T> {                                                                                                                                
  state: MutationState<T>;                                                                                                                                                               
  /** Ejecuta la operación y devuelve su resultado sin lanzar errores. */                                                                                                                
  run: (...args: A) => Promise<MutationOutcome<T>>;                                                                                                                                      
  /** Vuelve al estado inicial. */                                                                                                                                                       
  reset: () => void;                                                                                                                                                                     
}                                                                                                                                                                                        
                                                                                                                                                                                              
/**                                                                                                                                                                                      
* Gestiona una operación de escritura y evita envíos simultáneos.                                                                                                                       
*/                                                                                                                                                                                      
export function useApiMutation<A extends unknown[], T>(                                                                                                                                  
  action: (...args: A) => Promise<T>,                                                                                                                                                    
): MutationResult<A, T> {                                                                                                                                                                
  const [state, setState] = useState<MutationState<T>>({ status: "idle" });                                                                                                              
  const pending = useRef(false);                                                                                                                                                         
                                                                                                                                                                                              
  const run = useCallback(                                                                                                                                                               
    async (...args: A): Promise<MutationOutcome<T>> => {                                                                                                                                 
      if (pending.current) return { ok: false };
      
      pending.current = true;                                                                                                                                              
      setState({ status: "loading" });                                                                                                                                     
                                                                                                                                                                                              
      try {                                                                                                                                                                    
        const data = await action(...args);                                                                                                             
        setState({ status: "success", data });                                                                                                                                           
        return { ok: true, data };                                                                                                                                                       
      } catch (error) {                                                                                                                                                                  
        setState({ status: "error", error });                                                                                                                                            
        return { ok: false };                                                                                                                                                            
      } finally {                                                                                                                                                                        
        pending.current = false;                                                                                                                                                         
      }                                                                                                                                                                                  
    },                                                                                                                                                                                   
    [action],                                                                                                                                                                            
  );                                                                                                                                                                                     
                                                                                                                                                                                              
  const reset = useCallback(() => setState({ status: "idle" }), []);                                                                                                                     
                                                                                                                                                                                              
  return { state, run, reset };                                                                                                                                                          
} 