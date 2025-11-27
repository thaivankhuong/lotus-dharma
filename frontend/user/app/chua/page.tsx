import Link from "next/link";
import { PAGODAS } from "./data";

export default function PagodaListPage() {
  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-6">Danh Sách Chùa</h1>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {PAGODAS.map((pagoda) => (
          <Link
            key={pagoda.id}
            href={`/chua/${pagoda.id}`}
            className="block border p-6 rounded-xl hover:shadow-lg transition bg-white"
          >
            <h2 className="text-xl font-semibold">{pagoda.name}</h2>
            <p className="text-gray-600">{pagoda.location}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
