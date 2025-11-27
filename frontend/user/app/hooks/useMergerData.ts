import { useState, useEffect } from 'react';
import { MergerUnit, ProvinceListItem } from '../types/merger';

export function useMergerData() {
    const [provinces, setProvinces] = useState<ProvinceListItem[]>([]);
    const [selectedProvinceId, setSelectedProvinceId] = useState<string | null>(null);
    const [mergerUnits, setMergerUnits] = useState<MergerUnit[]>([]);
    const [loading, setLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);

    // Load province list on mount
    useEffect(() => {
        const fetchProvinces = async () => {
            try {
                const response = await fetch('/data/new-provinces/provinces_list.json');
                if (!response.ok) throw new Error('Failed to load province list');
                const data = await response.json();
                setProvinces(data);
            } catch (err) {
                console.error(err);
                setError('Không thể tải danh sách tỉnh');
            }
        };
        fetchProvinces();
    }, []);

    // Load details when province changes
    useEffect(() => {
        if (!selectedProvinceId) {
            setMergerUnits([]);
            return;
        }

        const fetchDetails = async () => {
            setLoading(true);
            try {
                const response = await fetch(`/data/new-provinces/details/${selectedProvinceId}.json`);
                if (!response.ok) throw new Error('Failed to load province details');
                const data = await response.json();
                setMergerUnits(data);
            } catch (err) {
                console.error(err);
                setError('Không thể tải dữ liệu chi tiết');
                setMergerUnits([]);
            } finally {
                setLoading(false);
            }
        };

        fetchDetails();
    }, [selectedProvinceId]);

    return {
        provinces,
        selectedProvinceId,
        setSelectedProvinceId,
        mergerUnits,
        loading,
        error
    };
}
