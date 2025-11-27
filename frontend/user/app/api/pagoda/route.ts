import { NextResponse } from "next/server";

const pagodas = [
  { id: 1, name: "Chùa One", province: "Đà Nẵng" },
  { id: 2, name: "Chùa Two", province: "Huế" },
  { id: 3, name: "Chùa Three", province: "Hà Nội" },
];

export async function GET() {
  return NextResponse.json(pagodas);
}
