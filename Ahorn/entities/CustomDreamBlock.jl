module CommunalHelper

using ..Ahorn

function renderCustomDreamBlock(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number, featherMode::Bool, oneUse::Bool)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)

    fillColor = featherMode ? (0.31, 0.69, 1.0, 0.4) : (0.0, 0.0, 0.0, 0.4)
	 lineColor = oneUse ? (1.0, 0.0, 0.0, 1.0) : (1.0, 1.0, 1.0, 1.0)
    Ahorn.drawRectangle(ctx, x, y, width, height, fillColor, lineColor)

    Ahorn.restore(ctx)
end

end