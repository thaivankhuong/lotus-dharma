async function getPagodas() {
  const res = await fetch("http://localhost:3000/api/pagoda", {
    cache: "no-store",   // Next.js 15: không cache API dev-time
  });
  return res.json();
}

export default async function PagodaListPage() {
  const pagodas = await getPagodas();

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-4">Danh sách chùa</h1>

      <ul className="space-y-3">
        {pagodas.map((p: any) => (
          <li key={p.id}>
            <a
              href={`/pagoda/${p.id}`}
              className="text-blue-600 underline"
            >
              {p.name} – {p.province}
            </a>
          </li>
        ))}
      </ul>
    </div>
  );
}
