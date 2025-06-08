const polygons = []

const polygonsAPI = {
    init: (json) => {
        const data = JSON.parse(json)
        polygons.length = 0
        polygons.push(...data)
    }
}

if (typeof mp !== "undefined") {
    const api = {
        "Polygons:API:init": polygonsAPI.init
    };

    for (const fn in api) {
        mp.events.add(fn, api[fn]);
    }
}

mp.events.add('render', () => {
    if (!polygons.length) return

    const player = mp.players.local
    const playerDim = player.dimension

    polygons.forEach(p => {
        if (!p.visible) return
        if (p.dimension && p.dimension !== playerDim && p.dimension !== -1) return
        if (!p.vertices || p.vertices.length < 2) return

        const color = p.lineColorRGBA ?? [255, 0, 0, 255]

        p.vertices.forEach((v, i) => {
            const next = i === p.vertices.length - 1 ? p.vertices[0] : p.vertices[i + 1]
            mp.game.graphics.drawLine(v.x, v.y, v.z, next.x, next.y, next.z,  color[0], color[1], color[2], color[3])
        })
        p.vertices.forEach((v, i) => {
            const next = i === p.vertices.length - 1 ? p.vertices[0] : p.vertices[i + 1]
            mp.game.graphics.drawLine(
                v.x, v.y, v.z + p.height,
                next.x, next.y, next.z + p.height,
                color[0], color[1], color[2], color[3]
            )
        })
        p.vertices.forEach(v => {
            mp.game.graphics.drawLine(
                v.x, v.y, v.z,
                v.x, v.y, v.z + p.height,
                color[0], color[1], color[2], color[3]
            )
        })
    })
})
