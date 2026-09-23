import { useEffect, useState } from 'react';
import { api } from '../api/client';
import { Link } from 'react-router-dom';
import { Search } from 'lucide-react';

export default function Tickets() {
  const [data, setData] = useState<any>({ items: [] });
  const [query, setQuery] = useState('');

  async function load() {
    const result = await api('/tickets?q=' + encodeURIComponent(query));
    setData(result);
  }

  useEffect(() => { void load(); }, []);

  return <>
    <div className="page-head"><div><h1>Tickets</h1><p>Manage conversations, ownership and resolution.</p></div><Link className="primary link" to="/tickets/new">New ticket</Link></div>
    <div className="panel">
      <div className="toolbar"><div className="search"><Search/><input placeholder="Search tickets or customers" value={query} onChange={e=>setQuery(e.target.value)} onKeyDown={e=>{if(e.key==='Enter') void load();}}/></div><button onClick={()=>void load()}>Search</button></div>
      <table><thead><tr><th>Ticket</th><th>Customer</th><th>Priority</th><th>Status</th><th>Assignee</th></tr></thead><tbody>{data.items.map((t:any)=><tr key={t.id}><td><Link to={'/tickets/'+t.id}><b>{t.title}</b><small>{t.reference}</small></Link></td><td>{t.customer}</td><td><span className={'badge '+String(t.priority).toLowerCase()}>{t.priority}</span></td><td>{pretty(t.status)}</td><td>{t.assignee||<span className="muted">Unassigned</span>}</td></tr>)}</tbody></table>
    </div>
  </>;
}
function pretty(x:string){return x.replace(/([A-Z])/g,' $1').trim();}
